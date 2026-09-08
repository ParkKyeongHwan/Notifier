using System.Text;
using System.Text.Json;

namespace Notifier
{
    public static class DiscordNotifier
    {
        private static string _webhookUrl = "https://discord.com/api/webhooks/1528427717387747391/8gOeqCG2F_tRmWT8Xnh6uaWfcrrPzP2EFftErU0aseDGoYp14PUhQRyBgQ3mtHCxxqAl";
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task SendMessageAsync(string message)
        {
            if (string.IsNullOrEmpty(_webhookUrl))
                return;

            // 디스코드는 "content"라는 키에 메시지를 담아 보냅니다.
            var payload = new { content = message };

            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                await _httpClient.PostAsync(_webhookUrl, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[디스코드 전송 실패] {ex.Message}");
            }
        }
    }
}
