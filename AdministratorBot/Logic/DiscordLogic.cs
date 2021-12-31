using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
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
        private readonly string BotUsername;
        private readonly string CommandPrefix;
        private readonly IAdminBotLogic _adminBotLogic;

        public DiscordLogic(IConfiguration configuration, IAdminBotLogic adminBotLogic)
        {
            BotUsername = configuration["auth:BotUsername"];
            CommandPrefix = configuration["options:prefix"];
            _adminBotLogic = adminBotLogic;
        }

        public async Task ProcessMessage(SocketMessage message)
        {
            string content = SanitizeContent(message.Content);
            string response = "";
            var mentionedBot = message.MentionedUsers.FirstOrDefault(x => x.Username == BotUsername) != null && message.Author.Username != BotUsername;
            var hasCommandPrefix = content.Substring(0, 1) == CommandPrefix;
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
