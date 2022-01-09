using AdministratorBot.Infrastructure;
using AdministratorBot.Logic;
using AdministratorBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace AdministratorBot
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(b =>
            {
                b.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true);
            }).ConfigureServices(services =>
                {
                    services.AddHostedService<DiscordHostedService>();
                    services.AddTransient<IAdminBotLogic, AdminBotLogic>();
                    services.AddTransient<IDiscordLogic, DiscordLogic>();
                    services.AddTransient<IIPWrapper, IPWrapper>();
                    services.AddTransient<IServerLogic, ServerLogic>();
                    services.AddTransient<IProcessWrapper, ProcessWrapper>();
                });
    }
}
