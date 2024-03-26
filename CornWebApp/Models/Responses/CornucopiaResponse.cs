using System.Text.Json.Serialization;

namespace CornWebApp.Models.Responses
{
    [method: JsonConstructor]
    public class CornucopiaResponse(bool success, string? message, long cornAdded, long cornTotal, string board, int matches)
    {
        public bool Success { get; set; } = success;
        public string? Message { get; set; } = message;
        public long CornAdded { get; set; } = cornAdded;
        public long CornTotal { get; set; } = cornTotal;
        public string Board { get; set; } = board;
        public int Matches { get; set; } = matches;
    }
}
