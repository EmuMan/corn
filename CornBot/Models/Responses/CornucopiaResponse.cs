using CornBot.Utilities;
using System.Text;
using System.Text.Json.Serialization;

namespace CornBot.Models.Responses
{
    [method: JsonConstructor]
    public class CornucopiaResponse(
        CornucopiaResponse.CornucopiaStatus status,
        string? message,
        long cornAdded,
        long cornTotal,
        List<string> board,
        int matches)
    {
        public enum CornucopiaStatus
        {
            Success,
            AlreadyClaimedMax,
            AmountTooLow,
            AmountTooHigh,
        }

        public CornucopiaStatus Status { get; set; } = status;
        public string? Message { get; set; } = message;
        public long CornAdded { get; set; } = cornAdded;
        public long CornTotal { get; set; } = cornTotal;
        public List<string> Board { get; set; } = board;
        public int Matches { get; set; } = matches;

        public int GetMaxWidth()
        {
            return Board.Max(row => row.Length);
        }

        public string RenderToString(long initialBet, CornucopiaInfoResponse info, int revealProgress)
        {
            var squareMap = new Dictionary<char, string>
            {
                { 'C', Events.GetCurrentEmoji() },
                { 'U', Constants.UNICORN_EMOJI },
                { 'P', Constants.POPCORN_EMOJI },
            };
            var name = Events.GetCurrentName();

            var sb = new StringBuilder();

            // header
            sb.AppendLine($"## **Cornucopia** ({info.CornucopiaCount + 1}/3)");
            sb.AppendLine($"### Bet: {initialBet:n0} {name}");
            sb.AppendLine();

            // slots grid
            for (int row = 0; row < Board.Count; row++)
            {
                sb.Append("# ");
                for (int col = 0; col < Board[row].Length; col++)
                {
                    if (col >= revealProgress)
                    {
                        sb.Append(Constants.LARGE_BLACK_SQUARE_EMOJI);
                    }
                    else
                    {
                        sb.Append(squareMap[Board[row][col]]);
                    }
                }
                sb.AppendLine();
            }

            // footer if all the board has been revealed
            if (revealProgress >= GetMaxWidth())
            {
                int matches = Matches;

                sb.AppendLine();
                sb.AppendLine();
                string lineStr = matches == 1 ? "line" : "lines";
                long netGain = CornAdded - initialBet;
                long absNetGain = Math.Abs(netGain);
                if (netGain == 0)
                    sb.AppendLine($"### You had {matches:n0} {lineStr} and your {name} remained the same.");
                else if (netGain < 0)
                    sb.AppendLine($"### You had {matches:n0} {lineStr} and lost {absNetGain:n0} {name}.");
                else
                    sb.AppendLine($"### You had {matches:n0} {lineStr} and won {absNetGain:n0} {name}!");
                sb.AppendLine();
                sb.AppendLine($"**You now have {CornTotal} {name}.**");
            }

            return sb.ToString();
        }
    }
}
