using Discord.WebSocket;

namespace AdministratorBot.Admin
{
    public class ServerActionModel
    {
        public ServerModel Server { get; set; }
        public ISocketMessageChannel Channel { get; set; }
    }
}
