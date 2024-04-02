using System.Text.Json.Serialization;

namespace CornBot.Models
{
    public class Guild(bool isNew, ulong guildId, int dailyCount, ulong announcementChannel)
    {
        public bool IsNew { get; set; } = isNew;
        public ulong GuildId { get; set; } = guildId;
        public int DailyCount { get; set; } = dailyCount;
        public ulong AnnouncementChannel { get; set; } = announcementChannel;

        [JsonConstructor]
        public Guild(bool isNew, ulong guildId)
            : this(isNew, guildId, 0, 0) { }
    }
}
