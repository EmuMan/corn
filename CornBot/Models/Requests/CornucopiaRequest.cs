using System.Text.Json.Serialization;

namespace CornBot.Models.Requests
{
    [method: JsonConstructor]
    public class CornucopiaRequest(long amount)
    {
        public long Amount { get; set; } = amount;
    }
}
