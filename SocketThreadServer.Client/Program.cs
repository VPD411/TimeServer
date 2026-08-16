using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketThreadServer.Client;

class Program
{
    static void Main()
    {
        // Создаём TCP-сокет
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            // Подключаемся к серверу localhost:13010
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 13010);
            clientSocket.Connect(serverEndPoint);
            Console.WriteLine("Подключено к серверу. Вводите сообщения (exit для выхода):");

            while (true)
            {
                string? message = Console.ReadLine();
                if (string.IsNullOrEmpty(message)) continue;

                // Добавляем перевод строки
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                clientSocket.Send(data);

                if (message.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Отправлен сигнал завершения.");
                    break;
                }

                // Получаем ответ
                byte[] buffer = new byte[1024];
                int received = clientSocket.Receive(buffer);
                string response = Encoding.UTF8.GetString(buffer, 0, received);

                Console.WriteLine($"Сервер: {response}");
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
        finally
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }

        Console.WriteLine("Нажмите любую клавишу...");
        Console.ReadKey();
    }
}