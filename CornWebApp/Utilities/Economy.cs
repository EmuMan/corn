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
                return new DailyResponse(DailyResponse.StatusCode.AlreadyClaimed,
                    "You have already claimed your daily corn", 0, user.CornCount);
            }

            var currentEvent = Events.GetCurrentEvent();
            var amount = (int)Math.Round(SimpleRNG.GetNormal(
                Constants.CORN_DAILY_MEAN, Constants.CORN_DAILY_STD_DEV));

            if (currentEvent == Constants.CornEvent.SHUCKING_STREAKS)
            {
                // TODO: Implement shucking streaks
            }

            user.CornCount += amount;
            user.HasClaimedDaily = true;
            guild.DailyCount += 1;

            if (Events.GetCurrentEvent() == Constants.CornEvent.SHARED_SHUCKING &&
                guild.DailyCount <= Constants.SHARED_SHUCKING_MAX_BONUS)
            {
                // TODO: Implement shared shucking
            }

            return new DailyResponse(DailyResponse.StatusCode.Success,
                "You have claimed your daily corn", amount, user.CornCount);
        }

        public static CornucopiaResponse PerformCornucopia(User user, long amount, Random random)
        {
            var maxAllowedAmount = GetCornucopiaMaxAmount(user);

            if (user.CornucopiaCount >= 3)
            {
                return new CornucopiaResponse(CornucopiaResponse.StatusCode.AlreadyClaimedMax,
                    "You have already performed the maximum number of Cornucopias today", 0, user.CornCount, [], 0);
            }

            if (amount < 1)
            {
                return new CornucopiaResponse(CornucopiaResponse.StatusCode.AmountTooLow,
                    "You must bet at least 1 corn", 0, user.CornCount, [], 0);
            }

            if (amount > maxAllowedAmount)
            {
                return new CornucopiaResponse(CornucopiaResponse.StatusCode.AmountTooHigh,
                    $"You can bet at most {maxAllowedAmount} corn", 0, user.CornCount, [], 0);
            }

            var slotMachine = new SlotMachine(3, amount, random);
            var winnings = slotMachine.GetWinnings();

            user.CornCount -= amount;
            user.CornCount += winnings;
            user.CornucopiaCount += 1;

            return new CornucopiaResponse(
                CornucopiaResponse.StatusCode.Success,
                "Cornucopia performed successfully",
                winnings,
                user.CornCount,
                slotMachine.GetBoard(),
                slotMachine.GetMatches().Values.Sum());
        }

        public static long GetCornucopiaMaxAmount(User user)
        {
            return (long)Math.Round(2_000.0 * user.CornCount / (user.CornCount + 2_000));
        }

        public static MessageResponse AddCornWithPenalty(User user, long amount)
        {
            // increase the corn multiplier by the time since the last edit
            var timeSinceLastEdit = DateTime.UtcNow - DateTime.FromBinary(user.CornMultiplierLastEdit);
            user.CornMultiplier = Math.Min(1.0, user.CornMultiplier + timeSinceLastEdit.TotalSeconds * (1.0 / Constants.CORN_RECHARGE_TIME));

            if (user.CornMultiplier <= 0.0)
            {
                // user is below cooldown threshold, don't give corn and max cooldown
                user.CornMultiplier = -1.0;
                user.CornMultiplierLastEdit = DateTime.UtcNow.ToBinary();
                return new MessageResponse(0);
            }
            // set penalty before corn is modified
            var penalty = (double)amount / 15;
            // set corn to use the modifier
            amount = (long)(Math.Round(amount * user.CornMultiplier));
            // apply penalty
            user.CornMultiplier -= penalty;
            user.CornCount += amount;
            user.CornMultiplierLastEdit = DateTime.UtcNow.ToBinary();
            return new MessageResponse(amount);
        }
    }
}
