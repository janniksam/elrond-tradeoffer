using Elrond.TradeOffer.Web.BotWorkflows;
using Elrond.TradeOffer.Web.BotWorkflows.BidsTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.UserState;
using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Network;
using Elrond.TradeOffer.Web.Repositories;
using Elrond.TradeOffer.Web.Services;
using Elrond.TradeOffer.Web.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.local.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

builder.Services.AddSingleton<IConfiguration>(configuration);

builder.Services.AddLogging(b =>
{
    b.AddConsole();
    var loggingSection = configuration.GetSection("Logging");
    b.AddFile(loggingSection,
        fileLoggerOpts =>
        {
            fileLoggerOpts.FormatLogFileName = fName => string.Format(fName, DateTime.UtcNow);
        });
});

// cache
builder.Services.AddSingleton<IUserCacheManager, UserCacheManager>();
builder.Services.AddSingleton<ITemporaryOfferManager, TemporaryOfferManager>();
builder.Services.AddSingleton<ITemporaryBidManager, TemporaryBidManager>();
builder.Services.AddSingleton<IBotManager, BotManager>();
builder.Services.AddSingleton<INetworkStrategies, NetworkStrategies>();
builder.Services.AddSingleton<IUserContextManager, UserContextManager>();

// db
var dataProvider = configuration.GetValue<string>("DataProvider");
builder.Services.AddDbContextFactory<ElrondTradeOfferDbContext>(o =>
{
    if (dataProvider == "memory")
    {
        o.UseInMemoryDatabase("ElrondTradeOffer")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }
    else 
    {
        var connectionString = configuration.GetValue<string>("MySqlConnectionString");
        o.UseMySql(new MySqlConnection(connectionString), ServerVersion.AutoDetect(new MySqlConnection(connectionString)));
    }
});

// services
builder.Services.AddTransient<IElrondApiService, ElrondApiService>();
builder.Services.AddTransient<ITransactionGenerator, TransactionGenerator>();
builder.Services.AddTransient<ITestDataProvider, TestDataProvider>();
builder.Services.AddTransient<IBotNotificationsHelper, BotNotificationsHelper>();
builder.Services.AddSingleton<IFeatureStatesManager, FeatureStatesManager>();

// repositories
builder.Services.AddTransient<IOfferRepository, SqlOfferRepository>();
builder.Services.AddTransient<IUserRepository, UserRepository>();
builder.Services.AddTransient<Func<IUserRepository>>(p => () => p.GetService<IUserRepository>()!);
builder.Services.AddTransient<Func<IOfferRepository>>(p => () => p.GetService<IOfferRepository>()!);

builder.Services.AddHostedService<ElrondTradeOfferBotService>();
builder.Services.AddHostedService<ElrondTradeStatusPollService>();

var app = builder.Build();

if (dataProvider == "memory")
{
    app.Services.GetService<ITestDataProvider>()?.ApplyAsync().Wait();
}
else
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ElrondTradeOfferDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{ 
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.Run();
