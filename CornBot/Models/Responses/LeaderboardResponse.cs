using CornBot.Utilities;
using System.Text;
using System.Text.Json.Serialization;

namespace CornBot.Models.Responses
{
    [method: JsonConstructor]
    public class LeaderboardResponse(List<Tuple<int, User>> leaderboard)
    {
        public List<Tuple<int, User>> Leaderboard { get; set; } = leaderboard;

        public LeaderboardResponse(List<User> userList)
            : this(new List<Tuple<int, User>>())
        {
            if (userList.Count == 0)
            {
                return;
            }
            long lastCornCount = userList[0].CornCount;
            int currentPlace = 1;
            for (int i = 0; i < userList.Count; i++)
            {
                if (userList[i].CornCount < lastCornCount)
                {
                    currentPlace = i + 1;
                    lastCornCount = userList[i].CornCount;
                }
                Leaderboard.Add(new Tuple<int, User>(currentPlace, userList[i]));
            }
        }

        public async Task<string> ToStringAsync(CornClient client)
        {
            var name = Events.GetCurrentName();
            var sb = new StringBuilder();
            foreach (Tuple<int, User> tuple in Leaderboard)
            {
                var suffix = tuple.Item2.HasClaimedDaily ? "" : $" {Constants.CALENDAR_EMOJI}";
                var displayString = await client.GetUserDisplayStringAsync(tuple.Item2.GuildId, tuple.Item2.UserId, true);
                sb.AppendLine($"{tuple.Item1} : {displayString} - {tuple.Item2.CornCount} {name}{suffix}");
            }
            return sb.ToString();
        }
    }
}
