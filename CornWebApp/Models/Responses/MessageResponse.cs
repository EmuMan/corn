using System.Text.Json.Serialization;

namespace CornWebApp.Models.Responses
{
    [method: JsonConstructor]
    public class MessageResponse(long amount)
    {
        public long Amount { get; set; } = amount;
    }
}
