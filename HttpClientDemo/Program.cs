using HttpClientDemo;
using System.Net.Http.Json;
using System.Text.Json;

class Program
{
    static async Task Main()
    {
        // Создаём HttpClient(в реальных проектах рекомендуется как singleton)
        using HttpClient client = new HttpClient();

        // URL публичного API
        string url = "https://jsonplaceholder.typicode.com/posts/2";

        try
        {
            // Асинхронно получаем объект напрямую
            Post? post = await client.GetFromJsonAsync<Post>(url);

            if (post != null)
            {
                Console.WriteLine($"Получен пост:\nID: {post.Id}\nUserId: {post.UserId}");
                Console.WriteLine($"Заголовок: {post.Title}");
                Console.WriteLine($"Текст: {post.Body}");
            }
            else
            {
                Console.WriteLine("Не удалось десериализовать объект.");
            }

            // Альтернативый вариант: получить строку и распарсить вручную
            string jsonString = await client.GetStringAsync(url);
            Console.WriteLine("Парсим сырой JSON...");

            // Выводим форматированный JSON
            using JsonDocument document = JsonDocument.Parse(jsonString);
            Console.WriteLine(JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Ошибка HTTP запроса: {e.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Необработанная ошибка: {ex.Message}");
        }

        Console.WriteLine("Нажмите любую кнопку для завершения...");
        Console.ReadKey();
    }
}