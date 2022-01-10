using AdministratorBot.Logic;
using AdministratorBot.Settings;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdministratorBot.Services
{
    public class DiscordHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<DiscordHostedService> _logger;
        private readonly DiscordSocketClient _client;
        private readonly IDiscordLogic _discordLogic;
        private readonly string _token;

        public DiscordHostedService(ILogger<DiscordHostedService> logger, IDiscordLogic discordLogic, IOptions<AuthOptions> authOptions)
        {
            _logger = logger;
            _discordLogic = discordLogic;
            if (authOptions.Value == null)
            {
                throw new Exception("Auth options were null!");
            }
            _token = authOptions.Value.Token;

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
            await _client.LoginAsync(TokenType.Bot, _token);
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
