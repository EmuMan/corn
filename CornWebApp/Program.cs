using CornWebApp.Utilities;
using CornWebApp.Connections;
using CornWebApp.Models;
using CornWebApp.Models.Responses;
using System.Text.Json.Serialization;
using CornWebApp.Models.Requests;

var builder = WebApplication.CreateSlimBuilder(args);
var appJsonSerializerContext = AppJsonSerializerContext.Default;
SimpleRNG.SetSeedFromSystemTime();
var random = new Random();

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
        var user = await database.GetUserAsync(guildId.Value, userId.Value);
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
    var user = await database.GetUserAsync(guildId, userId);
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
    var user = await database.GetOrCreateUserAsync(guildId, userId);
    var guild = await database.GetOrCreateGuildAsync(guildId);

    var result = Economy.PerformDaily(user, guild);

    await database.InsertOrUpdateUserAsync(user);
    await database.InsertOrUpdateGuildAsync(guild);

    return Results.Json(result, appJsonSerializerContext, statusCode: 201);
});

dailyApi.MapPost("/{guildId}/{userId}/reset", async (HttpContext context, ulong guildId, ulong userId) =>
{
    var user = await database.GetUserAsync(guildId, userId);

    if (user is null)
    {
        return Results.NotFound();
    }

    await database.ResetDailyAsync(user);

    return Results.NoContent();
});

dailyApi.MapPost("/{guildId}/reset", async (HttpContext context, ulong guildId) =>
{
    var guild = await database.GetGuildAsync(guildId);

    if (guild is null)
    {
        return Results.NotFound();
    }

    await database.ResetAllDailiesAsync(guild);

    return Results.NoContent();
});

dailyApi.MapPost("/reset", async (HttpContext context) =>
{
    await database.ResetAllDailiesAsync();

    return Results.NoContent();
});


var cornucopiaApi = app.MapGroup("/cornucopia");

cornucopiaApi.MapPost("/{guildId}/{userId}/perform", async (HttpContext context, ulong guildId, ulong userId) =>
{
    var request = await context.Request.ReadFromJsonAsync<CornucopiaRequest>();

    if (request is null)
    {
        return Results.BadRequest("Invalid cornucopia request");
    }

    var user = await database.GetOrCreateUserAsync(guildId, userId);
    var result = Economy.PerformCornucopia(user, request.Amount, random);

    await database.InsertOrUpdateUserAsync(user);

    return Results.Json(result, appJsonSerializerContext, statusCode: 201);
});

cornucopiaApi.MapPost("/{guildId}/{userId}/reset", async (HttpContext context, ulong guildId, ulong userId) =>
{
    var user = await database.GetUserAsync(guildId, userId);

    if (user is null)
    {
        return Results.NotFound();
    }

    await database.ResetCornucopiaAsync(user);

    return Results.NoContent();
});

cornucopiaApi.MapPost("/{guildId}/reset", async (HttpContext context, ulong guildId) =>
{
    var guild = await database.GetGuildAsync(guildId);

    if (guild is null)
    {
        return Results.NotFound();
    }

    await database.ResetAllCornucopiasAsync(guild);

    return Results.NoContent();
});

cornucopiaApi.MapPost("/reset", async (HttpContext context) =>
{
    await database.ResetAllCornucopiasAsync();

    return Results.NoContent();
});


app.Run();


[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(List<User>))]
[JsonSerializable(typeof(Guild))]
[JsonSerializable(typeof(DailyResponse))]
[JsonSerializable(typeof(CornucopiaResponse))]
[JsonSerializable(typeof(CornucopiaRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
