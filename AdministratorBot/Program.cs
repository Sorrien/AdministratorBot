using AdministratorBot.Infrastructure;
using AdministratorBot.Logic;
using AdministratorBot.Services;
using AdministratorBot.Settings;
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
            }).ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    
                    services.Configure<ServerOptions>(hostContext.Configuration.GetSection(ServerOptions.Server));
                    services.Configure<PermissionsOptions>(hostContext.Configuration.GetSection(PermissionsOptions.Permissions));
                    services.Configure<AuthOptions>(hostContext.Configuration.GetSection(AuthOptions.Auth));
                    services.Configure<CommandOptions>(hostContext.Configuration.GetSection(CommandOptions.Command));

                    services.AddHostedService<DiscordHostedService>();
                    services.AddTransient<IAdminBotLogic, AdminBotLogic>();
                    services.AddTransient<IDiscordLogic, DiscordLogic>();
                    services.AddTransient<IIPWrapper, IPWrapper>();
                    services.AddTransient<IServerLogic, ServerLogic>();
                    services.AddTransient<IProcessWrapper, ProcessWrapper>();
                });
    }
}
