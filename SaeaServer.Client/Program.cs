using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SaeaServer.Client;

class Program
{
    static async Task Main()
    {
        // Поднимаем сокеты
        Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Подключаемся к серверу localhost:13010
        await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 13010));

        Console.WriteLine("Успешно подключено к серверу.");

        // Отправляем несколько сообщений
        string[] testMessages = ["hello", "world", "sockets", "async "];
        foreach (var msg in testMessages)
        {
            byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
            byte[] lengthPrefix = BitConverter.GetBytes(msgBytes.Length);
            byte[] fullMessage = lengthPrefix.Concat(msgBytes).ToArray();
            await client.SendAsync(fullMessage, SocketFlags.None);
            Console.WriteLine($"Отправлено: {msg}");

            // Принимает ответ
            byte[] header = new byte[4];

            int received = await client.ReceiveAsync(header, SocketFlags.None);

            if (received == 4)
            {
                int responseLength = BitConverter.ToInt32(header, 0);
                byte[] responseBuffer = new byte[responseLength];
                int total = 0;
                while (total < responseLength)
                {
                    int r = await client.ReceiveAsync(responseBuffer.AsMemory(total, responseLength - total), SocketFlags.None);
                    total += r;
                }
                string response = Encoding.UTF8.GetString(responseBuffer);
                Console.WriteLine($"Ответ: {response}");
            }
        }

        client.Shutdown(SocketShutdown.Both);
        client.Close();
        Console.ReadKey();
    }
}