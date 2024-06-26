﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CornBot.Models
{
    public class User(
        bool isNew,
        ulong guildId,
        ulong userId,
        string? username,
        string? displayName,
        long cornCount,
        bool hasClaimedDaily,
        int cornucopiaCount,
        double cornMultiplier,
        ulong cornMultiplierLastEdit)
    {
        public bool IsNew { get; set; } = isNew;
        public ulong UserId { get; set; } = userId;
        public ulong GuildId { get; set; } = guildId;
        public string? Username { get; set; } = username;
        public string? DisplayName { get; set; } = displayName;
        public long CornCount { get; set; } = cornCount;
        public bool HasClaimedDaily { get; set; } = hasClaimedDaily;
        public int CornucopiaCount { get; set; } = cornucopiaCount;
        public double CornMultiplier { get; set; } = cornMultiplier;
        public ulong CornMultiplierLastEdit { get; set; } = cornMultiplierLastEdit;

        [JsonConstructor]
        public User(bool isNew, ulong guildId, ulong userId)
            : this(isNew, guildId, userId, 0, false, 0, 1.0, 0) { }

        public User(
            bool isNew,
            ulong guildId,
            ulong userId,
            long cornCount,
            bool hasClaimedDaily,
            int cornucopiaCount,
            double cornMultiplier,
            ulong cornMultiplierLastEdit)
            : this(
                  isNew,
                  guildId,
                  userId,
                  null,
                  null,
                  cornCount,
                  hasClaimedDaily,
                  cornucopiaCount,
                  cornMultiplier,
                  cornMultiplierLastEdit) { }
    }
}
