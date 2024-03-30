using System.Text.Json.Serialization;

namespace CornBot.Models.Responses
{
    [method: JsonConstructor]
    public class CornucopiaInfoResponse(
        long maxAmount,
        int cornucopiaCount)
    {
        public long MaxAmount { get; set; } = maxAmount;
        public int CornucopiaCount { get; set; } = cornucopiaCount;
    }
}
