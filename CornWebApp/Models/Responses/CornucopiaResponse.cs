using System.Text.Json.Serialization;

namespace CornWebApp.Models.Responses
{
    [method: JsonConstructor]
    public class CornucopiaResponse(
        CornucopiaResponse.CornucopiaStatus status,
        string? message,
        long cornAdded,
        long cornTotal,
        List<string> board,
        int matches)
    {
        public enum CornucopiaStatus
        {
            Success,
            AlreadyClaimedMax,
            AmountTooLow,
            AmountTooHigh,
        }

        public CornucopiaStatus Status { get; set; } = status;
        public string? Message { get; set; } = message;
        public long CornAdded { get; set; } = cornAdded;
        public long CornTotal { get; set; } = cornTotal;
        public List<string> Board { get; set; } = board;
        public int Matches { get; set; } = matches;
    }
}
