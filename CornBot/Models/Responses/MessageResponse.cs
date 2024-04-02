using System.Text.Json.Serialization;

namespace CornBot.Models.Responses
{
    [method: JsonConstructor]
    public class MessageResponse(long amount)
    {
        public long Amount { get; set; } = amount;
    }
}
