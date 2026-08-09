using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TimeServer.Server;

class Program
{
    static void Main()
    {
        // Зададим параметры соединения
        var port = 13000;
        UdpClient server = new UdpClient(port);
        Console.WriteLine($"UDP-сервер времени на порту {port}. Ожидание запросов...");

        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        try
        {
            while (true)
            {
                // Принимаем любую дейтаграмму
                byte[] requestData = server.Receive(ref remoteEndPoint);
                string requestText = Encoding.UTF8.GetString(requestData);
                Console.WriteLine($"Запрос от {remoteEndPoint}: {requestText}");

                // Формируем ответ с текущим временем
                string timeResponse = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                byte[] responseBytes = Encoding.UTF8.GetBytes(timeResponse);

                // Отправляем ответ обратно тому же клиенту
                server.Send(responseBytes, responseBytes.Length, remoteEndPoint);
                Console.WriteLine($"Отправлено время: {timeResponse} для {remoteEndPoint}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
        finally
        {
            server.Close();
        }
    }
}