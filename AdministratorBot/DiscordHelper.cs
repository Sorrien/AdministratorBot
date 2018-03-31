using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AdministratorBot.Admin;

namespace AdministratorBot
{
    public class DiscordHelper
    {
        public static IConfigurationRoot Configuration { get; set; }

        private DiscordSocketClient _client;
        private AdminBot adminBot;
        private string BotUsername;

        public DiscordHelper()
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", false, true)
             .AddJsonFile("authentication.json", false, false);

            Configuration = builder.Build();

            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info
            });
            _client.Log += Log;

            _client.MessageReceived += MessageReceived;

            adminBot = new AdminBot();
            adminBot.Startup();

            BotUsername = Configuration["auth:BotUsername"];
        }
        public async Task Init()
        {
            string token = Configuration["auth:token"];

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage message)
        {
            string content = SanitizeContent(message.Content);
            bool sfw = IsSafeForWork(content);
            string response = "";
            var mentionedBot = message.MentionedUsers.FirstOrDefault(x => x.Username == BotUsername) != null && message.Author.Username != BotUsername;
            var hasCommandPrefix = content.Substring(0, 1) == adminBot.CommandPrefix;
            //if (!sfw)
            //{
            //    await message.DeleteAsync();
            //}
            if (hasCommandPrefix || mentionedBot)
            {
                string command = content.Substring(1, content.Length - 1);
                response = await adminBot.RunCommand(command, message);
            }
            else if (mentionedBot)
            {
                //if (sfw)
                //{
                //    response = await chatbotService.GetChatResponseAsync(content, $"{message.Author.Username}{message.Author.Id}");
                //}
                //else
                //{
                //    response = "I don't feel comfortable talking about that.";
                //}
                //response = message.Author.Mention + " " + response;
            }
            if (!string.IsNullOrEmpty(response))
            {
                await message.Channel.SendMessageAsync(response);
            }
        }

        private bool IsSafeForWork(string content)
        {
            bool safe = true;
            var profanityList = Configuration.GetSection("Profanity").AsEnumerable();
            var sync = new Object();

            Parallel.ForEach(profanityList, (bad, loopState) =>
            {
                if (bad.Value != null && content.ToLower().Contains(bad.Value.ToLower()))
                {
                    lock (sync)
                    {
                        safe = false;
                        loopState.Stop();
                    }
                }
            });

            return safe;
        }

        private string SanitizeContent(string message)
        {
            string sanitized = message;
            sanitized = Regex.Replace(sanitized, "<.*?>", string.Empty);
            if (sanitized.Substring(0, 1) == " ")
            {
                sanitized = sanitized.Substring(1, sanitized.Length - 1);
            }
            return sanitized;
        }
    }
}
