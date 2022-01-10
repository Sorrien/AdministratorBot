namespace AdministratorBot.Admin
{
    public class ServerModel
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public bool HasLauncher { get; set; }
        public string Start { get; set; }
        public string StartArguments { get; set; }
        public string Update { get; set; }
        public string UpdateArguments { get; set; }
    }
}
