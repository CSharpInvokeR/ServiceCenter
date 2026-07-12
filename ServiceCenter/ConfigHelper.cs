using System.IO;
using System.Text.Json;

namespace ServiceCenter.Services
{
    public static class ConfigHelper
    {
        public static string GetConnectionString()
        {
            string json = File.ReadAllText("appsettings.json");
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement root = doc.RootElement;
                return root.GetProperty("ConnectionStrings")
                           .GetProperty("DefaultConnection")
                           .GetString();
            }
        }
        public static string GetFeedbackFormUrl()
        {
            string json = File.ReadAllText("appsettings.json");
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement root = doc.RootElement;
                return root.GetProperty("AppSettings")
                           .GetProperty("FeedbackFormUrl")
                           .GetString();
            }
        }
    }
}