using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketThreadServer.Client;

class Program
{
    static async Task Main()
    {
        // Создаём TCP-сокет
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            // Подключаемся к серверу localhost:13010
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 13010);
            await clientSocket.ConnectAsync(serverEndPoint);
            Console.WriteLine("Подключено к серверу. Вводите сообщения (exit для выхода):");

            // Задача приёма сообщений
            Task receiveTask = Task.Run(async () =>
            {
                byte[] buffer = new byte[1024];

                while (true)
                {
                    int received = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);
                    if (received == 0)
                    {
                        Console.WriteLine("Соединение закрыто сервером.");
                        break;
                    }

                    string response = Encoding.UTF8.GetString(buffer, 0, received);
                    Console.WriteLine($"Сервер: {response}");
                }
            });

            // Отправка сообщений
            while (true)
            {
                string? message = Console.ReadLine();
                if (string.IsNullOrEmpty(message)) continue;

                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                await clientSocket.SendAsync(data, SocketFlags.None);

                if (message.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                await receiveTask;
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
        Console.WriteLine("Нажмите любую клавишу...");
        Console.ReadKey();
    }
}