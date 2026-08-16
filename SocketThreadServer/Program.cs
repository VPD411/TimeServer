using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketThreadServer;

class Program
{
    static void Main()
    {
        // Создаём TCP-сокет
        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Привязываем сокет к localhost:13010
        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, 13010);
        listener.Bind(localEndPoint);

        // Начинаем слушать (максимум 10 ожидающих в очереди)
        listener.Listen(10);
        Console.WriteLine($"Сервер запущен на {localEndPoint}. Ожидаем подключений...");

        while (true)
        {
            // Блокируемся до нового подключения
            Socket clientSocket = listener.Accept();
            Console.WriteLine($"Подключён клиент: {clientSocket.RemoteEndPoint}");

            // Запускаем обработку клиента в отдельном потоке
            Thread clientThread = new Thread(() => HandleClient(clientSocket));
            clientThread.IsBackground = true;
            clientThread.Start();
        }
    }

    private static void HandleClient(Socket clientSocket)
    {
        byte[] buffer = new byte[1024];

        string clientInfo = clientSocket.RemoteEndPoint?.ToString() ?? "Undefined";

        try
        {
            while (true)
            {
                // Принимаем данные
                int received = clientSocket.Receive(buffer);
                if (received == 0)
                {
                    // Если длина равна нулю, значит пользователь закрыл соединение
                    Console.WriteLine($"Клиент {clientInfo} отключился.");
                    break;
                }

                string request = Encoding.UTF8.GetString(buffer, 0, received);
                Console.WriteLine($"[{clientInfo}] Получено: {request.TrimEnd('\r', '\n')}");

                // Проверка на выход
                if (request.TrimEnd('\r', '\n').Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Клиент {clientInfo} запросил завершение.");
                    break;
                }

                // Формируем ответ
                string response = $"Echo: {request}";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                // Отправляем обратно
                clientSocket.Send(responseBytes);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Ошибка сокета для {clientInfo}: {ex.Message}");
        }
        finally
        {
            // Корректно завершаем соединение
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }
}