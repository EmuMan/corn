using CornWebApp.Utilities;
using CornWebApp.Connections;
using CornWebApp.Models;
using CornWebApp.Models.Responses;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);
var appJsonSerializerContext = AppJsonSerializerContext.Default;
SimpleRNG.SetSeedFromSystemTime();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, appJsonSerializerContext);
});

var app = builder.Build();
var connString = "Server=tcp:cornbot.database.windows.net,1433;Initial Catalog=corndata;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";";
var database = new Database(connString);
await database.CreateTablesIfNotExistAsync();


var usersApi = app.MapGroup("/users");

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
        var user = await database.GetUserAsync(userId.Value, guildId.Value);
        return Results.Json(new { user }, appJsonSerializerContext);
    }
    else if (userId.HasValue)
    {
        var users = await database.GetUsersFromUserIdAsync(userId.Value);
        return Results.Json(users, appJsonSerializerContext);
    }
    else if (guildId.HasValue)
    {
        var users = await database.GetUsersFromGuildIdAsync(guildId.Value);
        return Results.Json(users, appJsonSerializerContext);
    }
    else
    {
        return Results.BadRequest("Missing userId or guildId");
    }
});

usersApi.MapGet("/{guildId}/{userId}", async (HttpContext context, ulong guildId, ulong userId) =>
{
    var user = await database.GetUserAsync(userId, guildId);
    if (user is null)
    {
        return Results.NotFound();
    }
    return Results.Json(user, appJsonSerializerContext);
});

usersApi.MapPost("/", async (HttpContext context) =>
{
    var user = await context.Request.ReadFromJsonAsync<User>();
    if (user is null)
    {
        return Results.BadRequest("Invalid user");
    }

    Console.WriteLine(user.CornMultiplierLastEdit);

    await database.InsertUserAsync(user);
    return Results.Created($"/users/{user.GuildId}/{user.UserId}", user);
});


var guildsApi = app.MapGroup("/guilds");

guildsApi.MapGet("/{guildId}", async (HttpContext context, ulong guildId) =>
{
    var guild = await database.GetGuildAsync(guildId);
    if (guild is null)
    {
        return Results.NotFound();
    }
    return Results.Json(guild, appJsonSerializerContext);
});

guildsApi.MapPost("/", async (HttpContext context) =>
{
    var guild = await context.Request.ReadFromJsonAsync<Guild>();
    if (guild is null)
    {
        return Results.BadRequest("Invalid guild");
    }

    await database.InsertGuildAsync(guild);
    return Results.Created($"/guilds/{guild.GuildId}", guild);
});


var dailyApi = app.MapGroup("/daily");

dailyApi.MapPost("/{guildId}/{userId}/complete", async (HttpContext context, ulong guildId, ulong userId) =>
{
    var user = await database.GetOrCreateUserAsync(userId, guildId);
    var guild = await database.GetOrCreateGuildAsync(guildId);

    var result = Economy.PerformDaily(user, guild);

    await database.InsertOrUpdateUserAsync(user);
    await database.InsertOrUpdateGuildAsync(guild);

    return Results.Json(result, appJsonSerializerContext, statusCode: 201);
});

dailyApi.MapPost("/{guildId}/{userId}/reset", async (HttpContext context, ulong guildId, ulong userId) =>
{
    var user = await database.GetUserAsync(userId, guildId);

    if (user is null)
    {
        return Results.NotFound();
    }

    user.HasClaimedDaily = false;

    await database.UpdateUserAsync(user);

    return Results.NoContent();
});

dailyApi.MapPost("/{guildId}/reset", async (HttpContext context, ulong guildId) =>
{
    await database.ResetAllDailiesAsync(guildId);

    return Results.NoContent();
});

dailyApi.MapPost("/reset", async (HttpContext context) =>
{
    await database.ResetAllDailiesAsync();

    return Results.NoContent();
});


app.Run();


[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(List<User>))]
[JsonSerializable(typeof(Guild))]
[JsonSerializable(typeof(DailyResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
