using System.Text.Json.Serialization;

namespace CornWebApp.Models
{
    public class Guild
    {
        public ulong GuildId { get; set; }
        public int DailyCount { get; set; }
        public ulong AnnouncementChannel { get; set; }

        [JsonConstructor]
        public Guild(ulong guildId)
            : this(guildId, 0, 0) { }

        public Guild(ulong guildId, int dailyCount, ulong announcementChannel)
        {
            GuildId = guildId;
            DailyCount = dailyCount;
            AnnouncementChannel = announcementChannel;
        }
    }
}
