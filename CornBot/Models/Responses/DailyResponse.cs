using System.Text.Json.Serialization;

namespace CornBot.Models.Responses
{
    [method: JsonConstructor]
    public class DailyResponse(DailyResponse.DailyStatus status, string? message, long cornAdded, long cornTotal)
    {
        public enum DailyStatus
        {
            Success,
            AlreadyClaimed,
        }
        
        public DailyStatus Status { get; set; } = status;
        public string? Message { get; set; } = message;
        public long CornAdded { get; set; } = cornAdded;
        public long CornTotal { get; set; } = cornTotal;
    }
}
