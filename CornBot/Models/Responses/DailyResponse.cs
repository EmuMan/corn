using System.Text.Json.Serialization;

namespace CornBot.Models.Responses
{
    [method: JsonConstructor]
    public class DailyResponse(DailyResponse.StatusCode status, string? message, long cornAdded, long cornTotal)
    {
        public enum StatusCode
        {
            Success,
            AlreadyClaimed,
        }

        public StatusCode Status { get; set; } = status;
        public string? Message { get; set; } = message;
        public long CornAdded { get; set; } = cornAdded;
        public long CornTotal { get; set; } = cornTotal;
    }
}
