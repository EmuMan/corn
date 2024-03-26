using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CornWebApp.Utilities
{
    public class Events
    {

        public static DateTimeOffset GetAdjustedTimestamp()
        {
            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            return new(now + Constants.TZ_OFFSET, Constants.TZ_OFFSET);
        }

        public static Constants.CornEvent GetCurrentEvent()
        {
            var timestamp = GetAdjustedTimestamp();
            return timestamp.Month switch
            {
                1 => Constants.CornEvent.SHARED_SHUCKING,
                2 => Constants.CornEvent.SHUCKING_STREAKS,
                3 => Constants.CornEvent.NORMAL_DISTRIBUTION_SHUCKING,
                6 => Constants.CornEvent.PRIDE,
                10 => timestamp.Day == 31 ? Constants.CornEvent.PUMPKIN : Constants.CornEvent.NONE,
                12 => timestamp.Day == 25 ? Constants.CornEvent.CHRISTMAS : Constants.CornEvent.NONE,
                _ => Constants.CornEvent.NONE,
            };
        }

        public static string GetCurrentEmoji()
        {
            switch (GetCurrentEvent()) {
                case Constants.CornEvent.PRIDE:
                    return Constants.PRIDE_CORN_EMOJI;
                case Constants.CornEvent.PUMPKIN:
                    return Constants.PUMPKIN_EMOJI;
                case Constants.CornEvent.CHRISTMAS:
                    return Constants.CHRISTMAS_CORN_EMOJI;
                default:
                    return Constants.CORN_EMOJI;
            }
        }

        public static string GetCurrentName()
        {
            switch (GetCurrentEvent())
            {
                case Constants.CornEvent.PUMPKIN:
                    return "pumpkins";
                default:
                    return "corn";
            }
        }

    }
}
