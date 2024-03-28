using CornWebApp.Database;
using CornWebApp.Models;
using CornWebApp.Models.Requests;
using CornWebApp.Utilities;

namespace CornWebApp
{
    public class Routes(WebApplication app, SqlDatabase database, AppJsonSerializerContext jsonSerializerContext, Random random)
    {
        public WebApplication App { get; private set; } = app;
        public SqlDatabase Database { get; private set; } = database;
        public AppJsonSerializerContext JsonSerializerContext { get; private set; } = jsonSerializerContext;
        public Random Random { get; private set; } = random;

        public void SetupAllRoutes()
        {
            SetupUserRoutes();
            SetupGuildRoutes();
            SetupDailyRoutes();
            SetupCornucopiaRoutes();
            SetupHistoryRoutes();
        }

        private void SetupUserRoutes()
        {
            var usersApi = App.MapGroup("/users");

            usersApi.MapGet("/", async (HttpContext context) =>
            {
                var userIdParam = context.Request.Query["userId"];
                var guildIdParam = context.Request.Query["guildId"];

                ulong? userId = null;
                if (!string.IsNullOrEmpty(userIdParam))
                {
                    if (!ulong.TryParse(userIdParam, out var pUserId))
                    {
                        return Results.BadRequest("Invalid userId");
                    }
                    userId = pUserId;
                }

                ulong? guildId = null;
                if (!string.IsNullOrEmpty(guildIdParam))
                {
                    if (!ulong.TryParse(guildIdParam, out var pGuildId))
                    {
                        return Results.BadRequest("Invalid guildId");
                    }
                    guildId = pGuildId;
                }

                if (userId.HasValue && guildId.HasValue)
                {
                    var user = await Database.Users.GetAsync(guildId.Value, userId.Value);
                    return Results.Json(new { user }, JsonSerializerContext);
                }
                else if (userId.HasValue)
                {
                    var users = await Database.Users.GetFromUserIdAsync(userId.Value);
                    return Results.Json(users, JsonSerializerContext);
                }
                else if (guildId.HasValue)
                {
                    var users = await Database.Users.GetFromGuildIdAsync(guildId.Value);
                    return Results.Json(users, JsonSerializerContext);
                }
                else
                {
                    return Results.BadRequest("Missing userId or guildId");
                }
            });

            usersApi.MapGet("/{guildId}/{userId}", async (HttpContext context, ulong guildId, ulong userId) =>
            {
                var user = await Database.Users.GetAsync(guildId, userId);
                if (user is null)
                {
                    return Results.NotFound();
                }
                return Results.Json(user, JsonSerializerContext);
            });

            usersApi.MapPost("/", async (HttpContext context) =>
            {
                var user = await context.Request.ReadFromJsonAsync<User>();
                if (user is null)
                {
                    return Results.BadRequest("Invalid user");
                }

                Console.WriteLine(user.CornMultiplierLastEdit);

                await Database.Users.InsertAsync(user);
                return Results.Created($"/users/{user.GuildId}/{user.UserId}", user);
            });
        }

        private void SetupGuildRoutes()
        {
            var guildsApi = App.MapGroup("/guilds");

            guildsApi.MapGet("/{guildId}", async (HttpContext context, ulong guildId) =>
            {
                var guild = await Database.Guilds.GetAsync(guildId);
                if (guild is null)
                {
                    return Results.NotFound();
                }
                return Results.Json(guild, JsonSerializerContext);
            });

            guildsApi.MapPost("/", async (HttpContext context) =>
            {
                var guild = await context.Request.ReadFromJsonAsync<Guild>();
                if (guild is null)
                {
                    return Results.BadRequest("Invalid guild");
                }

                await Database.Guilds.InsertAsync(guild);
                return Results.Created($"/guilds/{guild.GuildId}", guild);
            });
        }

        private void SetupDailyRoutes()
        {
            var dailyApi = App.MapGroup("/daily");

            dailyApi.MapPost("/{guildId}/{userId}/complete", async (HttpContext context, ulong guildId, ulong userId) =>
            {
                var user = await Database.Users.GetOrCreateAsync(guildId, userId);
                var guild = await Database.Guilds.GetOrCreateAsync(guildId);

                var result = Economy.PerformDaily(user, guild);

                await Database.Users.InsertOrUpdateAsync(user);
                await Database.Guilds.InsertOrUpdateAsync(guild);

                return Results.Json(result, JsonSerializerContext, statusCode: 201);
            });

            dailyApi.MapPost("/{guildId}/{userId}/reset", async (HttpContext context, ulong guildId, ulong userId) =>
            {
                var user = await Database.Users.GetAsync(guildId, userId);

                if (user is null)
                {
                    return Results.NotFound();
                }

                await Database.Users.ResetDailyAsync(user);

                return Results.NoContent();
            });

            dailyApi.MapPost("/{guildId}/reset", async (HttpContext context, ulong guildId) =>
            {
                var guild = await Database.Guilds.GetAsync(guildId);

                if (guild is null)
                {
                    return Results.NotFound();
                }

                await Database.Guilds.ResetAllDailiesAsync(guild);

                return Results.NoContent();
            });

            dailyApi.MapPost("/reset", async (HttpContext context) =>
            {
                await Database.ResetAllDailiesAsync();

                return Results.NoContent();
            });
        }

        private void SetupCornucopiaRoutes()
        {
            var cornucopiaApi = App.MapGroup("/cornucopia");

            cornucopiaApi.MapPost("/{guildId}/{userId}/perform", async (HttpContext context, ulong guildId, ulong userId) =>
            {
                var request = await context.Request.ReadFromJsonAsync<CornucopiaRequest>();

                if (request is null)
                {
                    return Results.BadRequest("Invalid cornucopia request");
                }

                var user = await Database.Users.GetOrCreateAsync(guildId, userId);
                var result = Economy.PerformCornucopia(user, request.Amount, Random);

                await Database.Users.InsertOrUpdateAsync(user);

                return Results.Json(result, JsonSerializerContext, statusCode: 201);
            });

            cornucopiaApi.MapPost("/{guildId}/{userId}/reset", async (HttpContext context, ulong guildId, ulong userId) =>
            {
                var user = await Database.Users.GetAsync(guildId, userId);

                if (user is null)
                {
                    return Results.NotFound();
                }

                await Database.Users.ResetCornucopiaAsync(user);

                return Results.NoContent();
            });

            cornucopiaApi.MapPost("/{guildId}/reset", async (HttpContext context, ulong guildId) =>
            {
                var guild = await Database.Guilds.GetAsync(guildId);

                if (guild is null)
                {
                    return Results.NotFound();
                }

                await Database.Guilds.ResetAllCornucopiasAsync(guild);

                return Results.NoContent();
            });

            cornucopiaApi.MapPost("/reset", async (HttpContext context) =>
            {
                await Database.ResetAllCornucopiasAsync();

                return Results.NoContent();
            });
        }

        private void SetupHistoryRoutes()
        {
            var historyApi = App.MapGroup("/history");

            historyApi.MapGet("/{userId}", async (HttpContext context, ulong userId) =>
            {
                var entries = await Database.History.GetFromUserIdAsync(userId);
                var history = new HistorySummary(entries);
                return Results.Json(history, JsonSerializerContext);
            });
        }
    }
}
