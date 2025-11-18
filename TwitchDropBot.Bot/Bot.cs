using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl;
using Quartz.Simpl;
using Serilog;
using TwitchDropBot.Bot.Helpers;
using TwitchDropBot.Bot.Quartz;
using TwitchDropBot.Service;

namespace TwitchDropBot.Bot;

public class Bot
{
    private ILogger<Bot> _logger;
    private DiscordSocketClient _client;
    
    private IScheduler _scheduler;
    private IServiceProvider _services;
    private ITrigger _trigger;
    private IJobDetail _job;
    
    private readonly Version _version = new Version(2025, 11, 18);

    static async Task Main(string[] args) => await new Bot().RunAsync();

    private async Task RunAsync()
    {
        AppDomain.CurrentDomain.UnhandledException += async (_, e) =>
        {
            _logger.LogError("{UnhandledExceptionEventArgs}\n{EExceptionObject}", e, e.ExceptionObject);
            await PostError($"Unhandled exception: {e.ExceptionObject}");
        };
        
        var provider = CreateProvider();
        _services = provider;
        
        _logger = provider.GetRequiredService<ILogger<Bot>>();
        _client = provider.GetRequiredService<DiscordSocketClient>();
        
        var token = (await File.ReadAllTextAsync("/run/secrets/botToken")).Trim();
        
        _client.Log += ClientOnLog;
        _client.Ready += ClientOnReady;
        _client.SlashCommandExecuted += async command =>
        {
            _logger.LogInformation("Command {CommandName} executed", command.Data.Name);
            switch (command.CommandName)
            {
                case "ignore":
                    await Commands.Commands.IgnoreGame(command, _client);
                    break;
                case "ignored-games":
                    await Commands.Commands.ListGames(command, true);
                    break;
                case "allowed-games":
                    await Commands.Commands.ListGames(command, false);
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

        await PostError(
            $"Bot error:\n{arg.Exception.Message}\n\n{arg.Exception.InnerException?.StackTrace}\n\n{arg.Message}");
        _logger.LogError(1, arg.Exception, "Bot error:\n{ExMessage}\n\n{InnerStackTrace}", arg.Exception.Message,
            arg.Exception.InnerException?.StackTrace);
    }
    
    private static IServiceProvider CreateProvider()
    {
        var config = new DiscordSocketConfig()
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
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>()
            .AddTransient<QuartzJob>() // Register QuartzJob
            .AddTransient<IDropService>(provider =>
        {
            var client = provider.GetRequiredService<DiscordSocketClient>();
            var logger = provider.GetRequiredService<ILogger<DropService>>();
            return new DropService(logger, client.Rest);
        });

        collection.AddLogging(configuration =>
        {
            configuration.ClearProviders();
            configuration.AddSerilog();
        });

        return collection.BuildServiceProvider();
    }

    private async Task PostError(string message)
    {
        var channel = await _client.GetChannelAsync(BotConfiguration.ErrorChannel) as ITextChannel
            ?? throw new Exception("Error channel not found");
        await channel.SendMessageAsync(message);
    }
}