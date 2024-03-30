using System.Text.Json.Serialization;

namespace CornWebApp.Models.Responses
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
    }
}
