using AdministratorBot.Admin;
using System.Collections.Generic;

namespace AdministratorBot.Settings
{
    public class ServerOptions
    {
        public const string Server = "ServerOptions";
        public List<ServerModel> Servers { get; set; }
    }
}