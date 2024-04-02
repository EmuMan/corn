using System.Text.Json.Serialization;

namespace CornWebApp.Models.Requests
{
    [method: JsonConstructor]
    public class MessageRequest(long amount)
    {
        public long Amount { get; set; } = amount;
    }
}
