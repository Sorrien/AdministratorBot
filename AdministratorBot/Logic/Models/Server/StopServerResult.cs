namespace AdministratorBot.Logic.Models.Server
{
    public record StopServerResult(StopServerResultType ResultType, string ResultMessage);
    public enum StopServerResultType
    {
        Success,
        StopProcessFailed,
        ProcessNotFound
    }
}
