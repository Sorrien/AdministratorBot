using System.Threading.Tasks;

namespace AdministratorBot
{
    class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            var discordHelper = new DiscordHelper();
            await discordHelper.Init();
            await Task.Delay(-1);
        }
    }
}
