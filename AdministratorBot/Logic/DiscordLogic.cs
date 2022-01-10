using AdministratorBot.Settings;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AdministratorBot.Logic
{
    public interface IDiscordLogic
    {
        Task ProcessMessage(SocketMessage message);
    }
    public class DiscordLogic : IDiscordLogic
    {
        private string _botUsername;
        private string _commandPrefix;
        private readonly IAdminBotLogic _adminBotLogic;

        public DiscordLogic(IOptionsSnapshot<AuthOptions> authOptions, IOptionsSnapshot<CommandOptions> commandOptions, IAdminBotLogic adminBotLogic)
        {
            if (authOptions.Value == null)
            {
                throw new Exception("auth config is null!");
            }
            _botUsername = authOptions.Value.BotUsername;

            if (commandOptions.Value == null)
            {
                throw new Exception("commands config is null!");
            }
            _commandPrefix = commandOptions.Value.Prefix;
            _adminBotLogic = adminBotLogic;
        }

        public async Task ProcessMessage(SocketMessage message)
        {
            string content = SanitizeContent(message.Content);
            string response = "";
            var mentionedBot = message.MentionedUsers.FirstOrDefault(x => x.Username == _botUsername) != null && message.Author.Username != _botUsername;
            var hasCommandPrefix = content.Substring(0, 1) == _commandPrefix;
            if (hasCommandPrefix || mentionedBot)
            {
                string command = content[1..];
                response = _adminBotLogic.RunCommand(command, message);
            }
            if (!string.IsNullOrWhiteSpace(response))
            {
                await message.Channel.SendMessageAsync(response);
            }
        }

        public static string SanitizeContent(string message)
        {
            string sanitized = message;
            sanitized = Regex.Replace(sanitized, "<.*?>", string.Empty);
            if (sanitized.Substring(0, 1) == " ")
            {
                sanitized = sanitized[1..];
            }
            return sanitized;
        }
    }
}
