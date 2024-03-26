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

        public static CornucopiaResponse PerformCornucopia(User user, long amount, Random random)
        {
            if (user.CornucopiaCount >= 3)
            {
                return new CornucopiaResponse(false, "You have already performed the maximum number of Cornucopias today", 0, user.CornCount, "", 0);
            }

            if (amount < 1)
            {
                return new CornucopiaResponse(false, "You must bet at least 1 corn", 0, user.CornCount, "", 0);
            }

            var maxAllowedAmount = (long)Math.Round(2_000.0 * user.CornCount / (user.CornCount + 2_000));
            if (amount > maxAllowedAmount)
            {
                return new CornucopiaResponse(false, $"You can bet at most {maxAllowedAmount} corn", 0, user.CornCount, "", 0);
            }

            var slotMachine = new SlotMachine(3, amount, random);
            var winnings = slotMachine.GetWinnings();

            user.CornCount -= amount;
            user.CornCount += winnings;
            user.CornucopiaCount += 1;

            return new CornucopiaResponse(
                true,
                "Cornucopia performed successfully",
                winnings,
                user.CornCount,
                slotMachine.GetStringRepresentation(),
                slotMachine.GetMatches().Values.Sum());
        }
    }
}
