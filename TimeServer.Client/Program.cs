using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TimeServer.Client;

class Program
{
    static void Main()
    {
        // Задаем параметры сервера
        string server = "127.0.0.1";
        int port = 13000;

        using UdpClient client = new UdpClient();

        try
        {
            // Отправляем пустой запрос (можем передать любую строку)
            byte[] request = Encoding.UTF8.GetBytes("time");
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(server), port);
            client.Send(request, request.Length, serverEndPoint);
            Console.WriteLine("Запрос времени отправлен.");

            // Ожидаем ответ (указываем ту же конечную точку для приема)
            IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
            byte[] response = client.Receive(ref remoteEp);
            string timeString = Encoding.UTF8.GetString(response);
            Console.WriteLine($"Текущее время на сервере: {timeString}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
        Console.ReadKey();
    }
}