using System.Text.Json.Serialization;

namespace CornWebApp.Models.Responses
{
    public class DailyResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long CornAdded { get; set; }
        public long CornTotal { get; set; }

        [JsonConstructor]
        public DailyResponse(bool success, string? message, long cornAdded, long cornTotal)
        {
            Success = success;
            Message = message;
            CornAdded = cornAdded;
            CornTotal = cornTotal;
        }
    }
}
