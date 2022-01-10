using AdministratorBot.Admin;
using System.Collections.Generic;

namespace AdministratorBot.Settings
{
    public class CommandOptions
    {
        public const string Command = "CommandOptions";
        public string Prefix { get; set; }
        public List<CommandModel> Commands { get; set; }
    }
}
