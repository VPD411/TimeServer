using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SaeaServer.Server;

/// <summary>
/// Класс для хранения состояния соединения.
/// </summary>
class ClientState
{
    /// <summary>
    /// Хранимый сокет.
    /// </summary>
    public Socket? Socket { get; set; }

    /// <summary>
    /// Фиксированный буфер приёма.
    /// </summary>
    public byte[] ReceiveBuffer { get; set; } = new byte[1024];

    /// <summary>
    /// Поток памяти, хранящий входящие данные.
    /// </summary>
    public MemoryStream? ReceiveStream { get; set; }

    /// <summary>
    /// Ожидаемая длина сообщения (-1 = ждём заголовок)
    /// </summary>
    public int ExpectedLength { get; set; } = -1;
}

class Program
{
    /// <summary>
    /// Потокобезопасная коллекция, хранящая в себе экземпляры приёма <see cref="SocketAsyncEventArgs"/>.
    /// </summary>
    private static readonly ConcurrentBag<SocketAsyncEventArgs> receiveEventArgsPool = [];

    /// <summary>
    /// Потокобезопасная коллекция, хранящая в себе экземпляры отправления <see cref="SocketAsyncEventArgs"/>.
    /// </summary>
    private static readonly ConcurrentBag<SocketAsyncEventArgs> sendEventArgsPool = [];

    /// <summary>
    /// Потокобезопасный словарь, где ключ - экземпляр сокета, а значение - привязанная к сокету модель состояния <see cref="ClientState"/>
    /// </summary>
    private static readonly ConcurrentDictionary<Socket, ClientState> clients = [];

    static void Main()
    {
        // Инициализируем сокет
        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Биндим сокет на localhost:13010
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 13010));

        listener.Listen(100);

        Console.WriteLine($"SAEA-сервер запущен на {listener.LocalEndPoint}");

        // Создаём SAEA для приёма подключений
        SocketAsyncEventArgs acceptArgs = new();
        acceptArgs.Completed += AcceptCompleted;

        StartAccept(listener, acceptArgs);

        Console.WriteLine("Нажмите Enter для остановки...");
        Console.ReadLine();
        listener.Close();

    }

    private static void StartAccept(Socket listener, SocketAsyncEventArgs acceptArgs)
    {
        // Сбрасываем сокет перед повторным использованием
        acceptArgs.AcceptSocket = null;
        bool willRaiseEvent = listener.AcceptAsync(acceptArgs);
        if (!willRaiseEvent)
        {
            ProcessAccept(acceptArgs);
        }
    }

    private static void AcceptCompleted(object? sender, SocketAsyncEventArgs e) => ProcessAccept(e);

    private static void ProcessAccept(SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            Socket clientSocket = e.AcceptSocket!;

            Console.WriteLine($"Подключён клиент: {clientSocket.RemoteEndPoint}");

            // Создаём состояние клиента и запускаем приём
            ClientState state = new()
            {
                Socket = clientSocket,
                ReceiveStream = new MemoryStream()
            };

            clients.TryAdd(clientSocket, state);

            // Получаем SAEA для приёма из пула и создаём новый
            SocketAsyncEventArgs receiveArgs = GetReceiveEventArgs();
            receiveArgs.UserToken = state;
            receiveArgs.SetBuffer(state.ReceiveBuffer, 0, state.ReceiveBuffer.Length);

            bool willRaise = clientSocket.ReceiveAsync(receiveArgs);
            if (!willRaise)
            {
                ProcessReceive(receiveArgs);
            }
        }

        // Продолжаем принимать следующие подключения
        // StartAccept((Socket)sender!, e);
    }

    private static SocketAsyncEventArgs GetReceiveEventArgs()
    {
        if (receiveEventArgsPool.TryTake(out var args))
        {
            return args;
        }

        var newArgs = new SocketAsyncEventArgs();
        newArgs.Completed += ReceiveCompleted;
        return newArgs;
    }

    private static void ReceiveCompleted(object? sender, SocketAsyncEventArgs e) => ProcessReceive(e);

    private static void ProcessReceive(SocketAsyncEventArgs e)
    {
        if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
        {
            // Клиент отключился или ошибка
            DisconnectClient(e);
            return;
        }

        var state = (ClientState)e.UserToken!;
        // Записываем полученные данные в накопительный поток
        state.ReceiveStream!.Write(state.ReceiveBuffer, 0, e.BytesTransferred);

        // Пытаемся извлечь полные сообщения
        ProcessReceivedData(state);

        // Продолжаем приём данных
        bool willRaise = state.Socket!.ReceiveAsync(e);
        if (!willRaise)
        {
            ProcessReceive(e);
        }
    }

    private static void ProcessReceivedData(ClientState state)
    {
        byte[] buffer = state.ReceiveStream!.ToArray();
        int offset = 0;

        while (buffer.Length > offset)
        {
            if (state.ExpectedLength == -1)
            {
                // Ждём заголовок (4 байта)
                if (buffer.Length - offset < 4)
                {
                    break; // Не хватает данных
                }

                state.ExpectedLength = BitConverter.ToInt32(buffer, offset);
                offset += 4;
            }
            else
            {
                // Ждём тело сообщения
                if (buffer.Length - offset < state.ExpectedLength)
                {
                    break; // Не хватает данных
                }

                byte[] messageBytes = new byte[state.ExpectedLength];
                Array.Copy(buffer, offset, messageBytes, 0, state.ExpectedLength);
                offset += state.ExpectedLength;

                // Обрабатываем сообщение
                string message = Encoding.UTF8.GetString(messageBytes);
                Console.WriteLine($"Получено: {message}");
                string response = message.ToUpperInvariant();
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                // Отправляем ответ
                SendResponse(state, responseBytes);

                // Сбрасываем ожидание длины для следующего сообщения
                state.ExpectedLength = -1;
            }
        }

        // Удаляем обработанные данные из потока
        if (offset > 0)
        {
            byte[] remaining = buffer.Skip(offset).ToArray();
            state.ReceiveStream = new MemoryStream(remaining);
        }
    }

    private static void SendResponse(ClientState state, byte[] responseBytes)
    {
        // Используем синхронную отправку (в реальном сервера тоже SAEA)
        try
        {
            // Добавляем префикс длины
            byte[] lengthPrefix = BitConverter.GetBytes(responseBytes.Length);
            byte[] fullMessage = lengthPrefix.Concat(responseBytes).ToArray();
            state.Socket!.Send(fullMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка отправки: {ex.Message}");
            DisconnectClient(state);
        }
    }

    private static void DisconnectClient(SocketAsyncEventArgs e)
    {
        var state = (ClientState)e.UserToken!;
        DisconnectClient(state);
        // Возвращем SAEA в пул
        receiveEventArgsPool.Add(e);
    }

    private static void DisconnectClient(ClientState state)
    {
        if (state.Socket != null)
        {
            Console.WriteLine($"Клиент {state.Socket.RemoteEndPoint} отключен");
            clients.TryRemove(state.Socket, out _);
            try
            {
                state.Socket.Shutdown(SocketShutdown.Both);
            }
            catch 
            {
                // Исключения глотаем
            }
            state.Socket.Close();
        }
    }
}