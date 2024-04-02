using CornWebApp.Utilities;

namespace CornWebApp.Models
{
    public class HistorySummary
    {
        public Dictionary<ulong, long> Total { get; set; } = [];
        public long TotalGlobal { get; set; } = 0;
        public Dictionary<ulong, int> DailyCount { get; set; } = [];
        public int DailyCountGlobal { get; set; } = 0;
        public Dictionary<ulong, long> DailyTotal { get; set; } = [];
        public long DailyTotalGlobal { get; set; } = 0;
        public Dictionary<ulong, long> DailyAverage { get; set; } = [];
        public long DailyAverageGlobal { get; set; } = 0;
        public Dictionary<ulong, int> LongestDailyStreak { get; set; } = [];
        public int LongestDailyStreakGlobal { get; set; } = 0;
        public Dictionary<ulong, int> CurrentDailyStreak { get; set; } = [];
        public int CurrentDailyStreakGlobal { get; set; } = 0;
        public Dictionary<ulong, int> MessageCount { get; set; } = [];
        public int MessageCountGlobal { get; set; } = 0;
        public Dictionary<ulong, long> MessageTotal { get; set; } = [];
        public long MessageTotalGlobal { get; set; } = 0;
        public Dictionary<ulong, int> CornucopiaCount { get; set; } = [];
        public int CornucopiaCountGlobal { get; set; } = 0;
        public Dictionary<ulong, long> CornucopiaTotal { get; set; } = [];
        public long CornucopiaTotalGlobal { get; set; } = 0;
        public Dictionary<ulong, long> CornucopiaAverage { get; set; } = [];
        public long CornucopiaAverageGlobal { get; set; } = 0;
        public Dictionary<ulong, double> CornucopiaPercent { get; set; } = [];
        public double CornucopiaPercentGlobal { get; set; } = 0;

        public HistorySummary(List<HistoryEntry> entries, List<Tuple<ulong, long>> totals)
        {
            var allGuildIds = entries.Select(e => e.GuildId).Distinct().ToList();

            Dictionary<ulong, List<HistoryEntry>> guildDailies = allGuildIds.ToDictionary(
                guildId => guildId,
                _ => new List<HistoryEntry>());
            Dictionary<ulong, List<HistoryEntry>> guildMessages = allGuildIds.ToDictionary(
                guildId => guildId,
                _ => new List<HistoryEntry>());
            Dictionary<ulong, List<Tuple<HistoryEntry, HistoryEntry>>> guildCornucopias = allGuildIds.ToDictionary(
                guildId => guildId,
                _ => new List<Tuple<HistoryEntry, HistoryEntry>>());
            List<HistoryEntry> allDailies = [];
            List<HistoryEntry> allMessages = [];
            List<Tuple<HistoryEntry, HistoryEntry>> allCornucopias = [];

            HistoryEntry? lastCornucopiaIn = null;

            foreach (var entry in entries)
            {
                if (entry.Type == HistoryEntry.ActionType.Daily)
                {
                    allDailies.Add(entry);
                    guildDailies[entry.GuildId].Add(entry);
                }
                else if (entry.Type == HistoryEntry.ActionType.Message)
                {
                    allMessages.Add(entry);
                    guildMessages[entry.GuildId].Add(entry);
                }
                else if (entry.Type == HistoryEntry.ActionType.CornucopiaIn)
                {
                    lastCornucopiaIn = entry;
                }
                else if (entry.Type == HistoryEntry.ActionType.CornucopiaOut)
                {
                    if (lastCornucopiaIn != null)
                    {
                        var pair = new Tuple<HistoryEntry, HistoryEntry>(lastCornucopiaIn, entry);
                        allCornucopias.Add(pair);
                        guildCornucopias[lastCornucopiaIn.GuildId].Add(pair);
                        lastCornucopiaIn = null;
                    }
                }
            }

            foreach (var total in totals)
            {
                Total[total.Item1] = total.Item2;
            }
            TotalGlobal = totals.Sum(t => t.Item2);

            ComputeDailyStats(guildDailies, allDailies);
            ComputeMessageStats(guildMessages, allMessages);
            ComputeCornucopiaStats(guildCornucopias, allCornucopias);
        }

        private static int ComputeLongestStreak(List<HistoryEntry> entries)
        {
            if (entries.Count == 0)
                return 0;

            int longestStreak = 0;
            int currentStreak = 0;
            DateTime currentDate = entries.Min(e => e.Timestamp).Date;
            DateTime maxDate = entries.Max(e => e.Timestamp).Date;

            while (currentDate <= maxDate)
            {
                if (entries.Any(e => e.Timestamp.Date == currentDate))
                {
                    currentStreak += 1;
                }
                else
                {
                    longestStreak = Math.Max(longestStreak, currentStreak);
                    currentStreak = 0;
                }

                currentDate = currentDate.AddDays(1);
            }

            return Math.Max(longestStreak, currentStreak);
        }

        private static int ComputeCurrentStreak(List<HistoryEntry> entries)
        {
            if (entries.Count == 0)
                return 0;

            int streak = 0;
            DateTime currentDate = Events.GetAdjustedTimestamp().Date;

            // the current day is optional for a streak
            if (entries.Any(e => e.Timestamp.Date == currentDate))
            {
                streak += 1;
            }
            currentDate = currentDate.AddDays(-1);

            while (entries.Any(e => e.Timestamp.Date == currentDate))
            {
                streak += 1;
                currentDate = currentDate.AddDays(-1);
            }

            return streak;
        }

        private static double ComputePercentGains(List<Tuple<HistoryEntry, HistoryEntry>> pairs)
        {
            var totalIn = pairs.Sum(pair => pair.Item1.Value);
            var totalOut = pairs.Sum(pair => pair.Item2.Value);
            return (totalOut - totalIn) / totalIn;
        }

        private void ComputeDailyStats(
            Dictionary<ulong, List<HistoryEntry>> guildDailies,
            List<HistoryEntry> allDailies)
        {
            DailyCount = guildDailies.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count);
            DailyCountGlobal = allDailies.Count;

            DailyTotal = guildDailies.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Sum(e => e.Value));
            DailyTotalGlobal = allDailies.Sum(e => e.Value);

            DailyAverage = guildDailies.ToDictionary(
                kvp => kvp.Key,
                kvp => DailyCount[kvp.Key] == 0 ? 0 : DailyTotal[kvp.Key] / DailyCount[kvp.Key]);
            DailyAverageGlobal = DailyCountGlobal == 0 ? 0 : DailyTotalGlobal / DailyCountGlobal;

            LongestDailyStreak = guildDailies.ToDictionary(
                kvp => kvp.Key,
                kvp => ComputeLongestStreak(kvp.Value));
            LongestDailyStreakGlobal = ComputeLongestStreak(allDailies);

            CurrentDailyStreak = guildDailies.ToDictionary(
                kvp => kvp.Key,
                kvp => ComputeCurrentStreak(kvp.Value));
            CurrentDailyStreakGlobal = ComputeCurrentStreak(allDailies);
        }

        private void ComputeMessageStats(
            Dictionary<ulong, List<HistoryEntry>> guildMessages,
            List<HistoryEntry> allMessages)
        {
            MessageCount = guildMessages.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count);
            MessageCountGlobal = allMessages.Count;

            MessageTotal = guildMessages.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Sum(e => e.Value));
            MessageTotalGlobal = allMessages.Sum(e => e.Value);
        }

        private void ComputeCornucopiaStats(
            Dictionary<ulong, List<Tuple<HistoryEntry, HistoryEntry>>> guildCornucopias,
            List<Tuple<HistoryEntry, HistoryEntry>> allCornucopias)
        {
            CornucopiaCount = guildCornucopias.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count);
            CornucopiaCountGlobal = allCornucopias.Count;

            CornucopiaTotal = guildCornucopias.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Sum(pair => pair.Item2.Value - pair.Item1.Value));
            CornucopiaTotalGlobal = allCornucopias.Sum(pair => pair.Item2.Value - pair.Item1.Value);

            CornucopiaAverage = guildCornucopias.ToDictionary(
                kvp => kvp.Key,
                kvp => CornucopiaCount[kvp.Key] == 0 ? 0 : CornucopiaTotal[kvp.Key] / CornucopiaCount[kvp.Key]);
            CornucopiaAverageGlobal = CornucopiaCountGlobal == 0 ? 0 : CornucopiaTotalGlobal / CornucopiaCountGlobal;

            CornucopiaPercent = guildCornucopias.ToDictionary(
                kvp => kvp.Key,
                kvp => ComputePercentGains(kvp.Value));
            CornucopiaPercentGlobal = ComputePercentGains(allCornucopias);
        }
    }
}
