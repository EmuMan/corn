﻿using CornWebApp.Models.Requests;
using CornWebApp.Models.Responses;
using CornWebApp.Models;
using System.Text.Json.Serialization;

namespace CornWebApp.Utilities
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
