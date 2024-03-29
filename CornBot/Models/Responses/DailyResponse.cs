using System.Text.Json.Serialization;

namespace CornBot.Models.Responses
{
    [method: JsonConstructor]
    public class DailyResponse(bool success, string? message, long cornAdded, long cornTotal)
    {
        public bool Success { get; set; } = success;
        public string? Message { get; set; } = message;
        public long CornAdded { get; set; } = cornAdded;
        public long CornTotal { get; set; } = cornTotal;
    }
}
