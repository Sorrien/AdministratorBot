using AdministratorBot.Logic;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdministratorBot.Services
{
    public class DiscordHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<DiscordHostedService> _logger;
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _configuration;
        private readonly IDiscordLogic _discordLogic;

        public DiscordHostedService(ILogger<DiscordHostedService> logger, IConfiguration configuration, IDiscordLogic discordLogic)
        {
            _logger = logger;
            _configuration = configuration;
            _discordLogic = discordLogic;

            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info
            });
            _client.Log += Log;
            _client.MessageReceived += MessageReceived;
        }

        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            string token = _configuration["auth:token"];

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.StopAsync();
        }

        public Task Log(LogMessage msg)
        {
            var logLevel = msg.Severity switch
            {
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Verbose or LogSeverity.Debug => LogLevel.Debug,
                _ => LogLevel.Information,
            };
            _logger.Log(logLevel, msg.ToString());
            return Task.CompletedTask;
        }

        public async Task MessageReceived(SocketMessage message)
        {
            await _discordLogic.ProcessMessage(message);
        }
    }
}
