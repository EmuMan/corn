using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CornWebApp.Models
{
    public class User
    {
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public long CornCount { get; set; }
        public bool HasClaimedDaily { get; set; }
        public double CornMultiplier { get; set; }
        public ulong CornMultiplierLastEdit { get; set; }

        [JsonConstructor]
        public User(ulong guildId, ulong userId)
            : this(guildId, userId, 0, false, 1.0, 0) { }

        public User(
            ulong guildId,
            ulong userId,
            long cornCount,
            bool hasClaimedDaily,
            double cornMultiplier,
            ulong cornMultiplierLastEdit)
            : this(
                  guildId,
                  userId,
                  null,
                  null,
                  cornCount,
                  hasClaimedDaily,
                  cornMultiplier,
                  cornMultiplierLastEdit) { }

        public User(
            ulong guildId,
            ulong userId,
            string? username,
            string? displayName,
            long cornCount,
            bool hasClaimedDaily,
            double cornMultiplier,
            ulong CornMultiplierLastEdit)
        {
            GuildId = guildId;
            UserId = userId;
            Username = username;
            DisplayName = displayName;
            CornCount = cornCount;
            HasClaimedDaily = hasClaimedDaily;
            CornMultiplier = cornMultiplier;
            CornMultiplierLastEdit = CornMultiplierLastEdit;
        }
    }
}
