namespace AdministratorBot.Admin
{
    public record ServerModel(string Name, string Address, int Port, bool HasLauncher, string Start, string StartArguments, string Update, string UpdateArguments);
}
