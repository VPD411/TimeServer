using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketThreadServer.Server;

class Program
{
    private static readonly CancellationTokenSource cts = new();

    static async Task Main()
    {
        // Обработка Ctrl+C
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // Поднимаем сокет
        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 13010));
        listener.Listen(100);
        Console.WriteLine($"Асинхронный сервер запущен на {listener.LocalEndPoint}. Нажмите Ctrl+C для остановки.");

        try
        {
            while (!cts.IsCancellationRequested)
            {
                // Асинхронно ожидаем подключение
                Socket clientSocket = await listener.AcceptAsync(cts.Token);
                Console.WriteLine($"Подключен клиент: {clientSocket.RemoteEndPoint}");

                // Запускаем обработку клиента (не блокируем цикл приема)
                _ = HandleClientAsync(clientSocket, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Остановка сервера...");
        }
        finally
        {
            listener.Close();
        }
    }

    private static async Task HandleClientAsync(Socket clientSocket, CancellationToken token)
    {
        byte[] buffer = new byte[1024];
        string clientInfo = clientSocket.RemoteEndPoint?.ToString() ?? "Undefined";

        try
        {
            while (true)
            {
                // Асинхронно принимаем данные
                int received = await clientSocket.ReceiveAsync(buffer, SocketFlags.None, token);

                // Разрыв соединения
                if (received == 0)
                {
                    Console.WriteLine($"Клиент {clientInfo} отсоединился.");
                }

                string request = Encoding.UTF8.GetString(buffer, 0, received);
                Console.WriteLine($"[{clientInfo} Получено: {request.TrimEnd('\r', '\n')}");

                if (request.TrimEnd('\r', '\n').Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    // Запрошен exit
                    break;
                }

                // Формируем ответ
                string response = $"Echo: {request}";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                // Асинхронно отправляем ответ
                await clientSocket.SendAsync(responseBytes, SocketFlags.None, token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Обработка клиента {clientInfo} отменена.");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Ошибка сокета для {clientInfo}: {ex.Message}");
        }
        finally
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }
}