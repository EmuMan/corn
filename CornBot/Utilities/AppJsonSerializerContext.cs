using CornBot.Models;
using CornBot.Models.Requests;
using CornBot.Models.Responses;
using System.Text.Json.Serialization;

namespace CornBot.Utilities
{
    [JsonSerializable(typeof(User))]
    [JsonSerializable(typeof(List<User>))]
    [JsonSerializable(typeof(Guild))]
    [JsonSerializable(typeof(List<Guild>))]
    [JsonSerializable(typeof(DailyResponse))]
    [JsonSerializable(typeof(CornucopiaResponse))]
    [JsonSerializable(typeof(CornucopiaInfoResponse))]
    [JsonSerializable(typeof(LeaderboardResponse))]
    [JsonSerializable(typeof(MessageResponse))]
    [JsonSerializable(typeof(CornucopiaRequest))]
    [JsonSerializable(typeof(MessageRequest))]
    [JsonSerializable(typeof(HistorySummary))]
    public partial class AppJsonSerializerContext : JsonSerializerContext
    {

    }
}
