using CornWebApp.Utilities;
using CornWebApp.Database;
using CornWebApp;

var builder = WebApplication.CreateSlimBuilder(args);
var appJsonSerializerContext = AppJsonSerializerContext.Default;
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, appJsonSerializerContext);
});
var app = builder.Build();

var connString = "Server=tcp:cornbot.database.windows.net,1433;Initial Catalog=corndata;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";";
var database = new SqlDatabase(connString);
await database.CreateTablesIfNotExistAsync();

SimpleRNG.SetSeedFromSystemTime();
var random = new Random();

var routes = new Routes(app, database, appJsonSerializerContext, random);
routes.SetupAllRoutes();

app.Run();
