using System.Text.Json.Serialization;

namespace CornWebApp.Models
{
    public class HistoryEntry(
        int id,
        ulong userId,
        ulong guildId,
        HistoryEntry.ActionType actionType,
        long value,
        DateTime timestamp)
    {
        public enum ActionType
        {
            Miscellaneous,
            Administrative,
            Message,
            Daily,
            CornucopiaIn,
            CornucopiaMatches,
            CornucopiaOut,
        }

        public int Id { get; set; } = id;
        public ulong UserId { get; set; } = userId;
        public ulong GuildId { get; set; } = guildId;
        public ActionType Type { get; set; } = actionType;
        public long Value { get; set; } = value;
        public DateTime Timestamp { get; set; } = timestamp;
    }
}
