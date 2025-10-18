﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using TwitchDropBot.Service;

namespace TwitchDropBot.Bot.Quartz;

public class QuartzJob(
    ILogger<QuartzJob> logger,
    IDropService dropService,
    IConfiguration configuration) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Getting drops");
        var drops = await dropService.GetDrops();
        logger.LogInformation("Got {dropsCount} drops", drops.Count);
        foreach (var drop in drops)
        {
            await dropService.PostDrop(configuration["postChannel"], drop);
        }
    }
}