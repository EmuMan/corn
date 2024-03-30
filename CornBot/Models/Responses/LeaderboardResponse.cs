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

        public override string ToString()
        {
            var name = Events.GetCurrentName();
            var sb = new StringBuilder();
            foreach (Tuple<int, User> tuple in Leaderboard)
            {
                var suffix = tuple.Item2.HasClaimedDaily ? "" : $" {Constants.CALENDAR_EMOJI}";
                // TODO: add user display name
                sb.Append($"{tuple.Item1} : {tuple.Item2.UserId} - {tuple.Item2.CornCount} {name}{suffix}");
            }
            return sb.ToString();
        }
    }
}
