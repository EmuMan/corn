using CornWebApp.Utilities;
using CornWebApp.Models;
using CornWebApp.Models.Responses;

namespace CornWebApp.Utilities
{
    public static class Economy
    {
        public static DailyResponse PerformDaily(User user, Guild guild)
        {
            if (user.HasClaimedDaily)
            {
                return new DailyResponse(false, "You have already claimed your daily corn", 0, user.CornCount);
            }

            var currentEvent = Events.GetCurrentEvent();
            var amount = (int)Math.Round(SimpleRNG.GetNormal(
                Constants.CORN_DAILY_MEAN, Constants.CORN_DAILY_STD_DEV));

            if (currentEvent == Constants.CornEvent.SHUCKING_STREAKS)
            {
                // TODO: Implement history
            }

            user.CornCount += amount;
            user.HasClaimedDaily = true;
            guild.DailyCount += 1;

            if (Events.GetCurrentEvent() == Constants.CornEvent.SHARED_SHUCKING &&
                guild.DailyCount <= Constants.SHARED_SHUCKING_MAX_BONUS)
            {
                // TODO: Implement shared shucking
            }
                
            // TODO: Implement history/logging

            return new DailyResponse(true, "You have claimed your daily corn", amount, user.CornCount);
        }
    }
}
