using CornBot.Models;
using CornBot.Utilities;

namespace CornBot.Models
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
    }
}
