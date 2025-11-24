using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl;
using Quartz.Simpl;
using Serilog;
using TwitchDropBot.Bot.Helpers;
using TwitchDropBot.Bot.Quartz;
using TwitchDropBot.Data;
using TwitchDropBot.Service;

namespace TwitchDropBot.Bot;

public class Bot
{
    private ILogger<Bot> _logger;
    private DiscordSocketClient _client;
    
    private IBotService _botService;
    private IDropService _dropService;
    
    private IScheduler _scheduler;
    private IServiceProvider _services;
    private ITrigger _trigger;
    private IJobDetail _job;
    
    private readonly Version _version = new(2025, 11, 21, 1);

    private static async Task Main(string[] args) => await new Bot().RunAsync();

    private async Task RunAsync()
    {
        AppDomain.CurrentDomain.UnhandledException += async (_, e) =>
        {
            _logger.LogError("{UnhandledExceptionEventArgs}\n{EExceptionObject}", e, e.ExceptionObject);
            await _botService.SendToErrorAsync($"Unhandled exception: {e.ExceptionObject}");
        };
        
        _services = CreateProvider();
        // ensure database exists
        await _services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        
        _logger = _services.GetRequiredService<ILogger<Bot>>();
        _client = _services.GetRequiredService<DiscordSocketClient>();
        
        _botService = _services.GetRequiredService<IBotService>();
        _dropService = _services.GetRequiredService<IDropService>();
        
        var token = Environment.GetEnvironmentVariable("BotToken") 
                    ?? throw new Exception("Bot token not found. Set the token in compose.yml");
        
        _client.Log += ClientOnLog;
        _client.Ready += ClientOnReady;
        _client.SlashCommandExecuted += async command =>
        {
            _logger.LogInformation("Command {CommandName} executed", command.Data.Name);
            var c = new Commands.Commands(_botService, _dropService);
            switch (command.CommandName)
            {
                case "ignore":
                    await c.IgnoreGame(command, _client);
                    break;
                case "list-games":
                    await c.ListGames(command);
                    break;
                case "check-twitch":
                    if (!_botService.IsAdmin(command.User.Id))
                    {
                        await command.RespondAsync("You do not have permission to use this command");
                        await _botService.SendToErrorAsync($"User {command.User.Username} tried to use check command");
                        break;
                    }
                    await _scheduler.TriggerJob(_job.Key);
                    await command.RespondAsync("Scheduled check.", ephemeral: true);
                    break;
                case "get-drops":
                    await c.GetDrops(command);
                    break;
            }
        };
        
        
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.SetGameAsync($"v{_version}");
        await _client.StartAsync();
        
        _logger.LogInformation("Started");
        await Task.Delay(-1);
    }

    private async Task ClientOnReady()
    {
        // register commands
        _logger.LogInformation($"Updating commands");
        _logger.LogTrace(string.Join(" | ", Commands.Commands.CommandBuilders.Select(c => c.Name)));
        await _client.BulkOverwriteGlobalApplicationCommandsAsync(
            Commands.Commands.CommandBuilders.Select(b => b.Build()).ToArray<ApplicationCommandProperties>());
        
        _scheduler = await StdSchedulerFactory.GetDefaultScheduler();
        _scheduler.JobFactory =
            new MicrosoftDependencyInjectionJobFactory(_services, new OptionsWrapper<QuartzOptions>(null));
        await _scheduler.Start();
        _job = JobBuilder.Create<QuartzJob>()
            .WithIdentity("QuartzJob", "Twitch")
            .Build();
        _trigger = TriggerBuilder.Create()
            .WithIdentity("QuartzTrigger", "Twitch")
            .WithSimpleSchedule(x => x.WithIntervalInHours(2).RepeatForever())
            .Build();
        
        await _scheduler.ScheduleJob(_job, _trigger);
    }

    private async Task ClientOnLog(LogMessage arg)
    {
        _logger.LogInformation(arg.Message);
        if (null == arg.Exception ||
            arg.Message.Contains("Server requested a reconnect") ||
            arg.Message.Contains("WebSocket connection was closed"))
        {
            return;
        }

        await _botService.SendToErrorAsync($"Bot error:\n{arg.Exception.Message}\n\n{arg.Exception.InnerException?.StackTrace}\n\n{arg.Message}");
        _logger.LogError(1, arg.Exception, "Bot error:\n{ExMessage}\n\n{InnerStackTrace}", arg.Exception.Message,
            arg.Exception.InnerException?.StackTrace);
    }
    
    private static IServiceProvider CreateProvider()
    {
        var discordSocketConfig = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.GuildMessages
        };
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("./logs/log.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var collection = new ServiceCollection();

        collection
            .AddSingleton(discordSocketConfig)
            .AddSingleton<DiscordSocketClient>()
            .AddTransient<QuartzJob>() // Register QuartzJob
            .AddTransient<IBotService>(provider =>
            {
                var client = provider.GetRequiredService<DiscordSocketClient>();
                return new BotService(client.Rest, BotConfiguration.PostChannels, BotConfiguration.ErrorChannels, BotConfiguration.AdminUsers);
            })
            .AddTransient<IDropService>(provider =>
        {
            var client = provider.GetRequiredService<DiscordSocketClient>();
            var logger = provider.GetRequiredService<ILogger<DropService>>();
            var botService = provider.GetRequiredService<IBotService>();
            return new DropService(logger, client.Rest, botService);
        })
            .AddSingleton<IBotService>(provider =>
        {
            var rest = provider.GetRequiredService<DiscordSocketClient>().Rest;
            return new BotService(rest, BotConfiguration.PostChannels, BotConfiguration.ErrorChannels, BotConfiguration.AdminUsers);
        })
            .AddDbContext<DropContext>()
            .AddSingleton<DatabaseInitializer>();

        collection.AddLogging(configuration =>
        {
            configuration.ClearProviders();
            configuration.AddSerilog();
        });

        return collection.BuildServiceProvider();
    }
}