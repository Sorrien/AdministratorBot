using AdministratorBot.Admin;
using AdministratorBot.Infrastructure;
using AdministratorBot.Logic.Models.Server;
using AdministratorBot.Settings;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AdministratorBot.Logic
{
    public interface IServerLogic
    {
        ServerModel GetServer(string serverName);
        ServerStatus GetServerStatus(ServerModel server);
        StartServerResult StartServer(ServerModel server);
        StopServerResult StopServer(ServerModel server);
        bool UpdateServer(ServerModel server, ISocketMessageChannel channel);
        List<ServerModel> GetServers();
        bool IsServerOnline(ServerModel server);
    }
    public class ServerLogic : IServerLogic
    {
        private List<ServerModel> _servers;
        private readonly IProcessWrapper _processLogic;
        private readonly IIPWrapper _ipLogic;
        private readonly ILogger<ServerLogic> _logger;

        public ServerLogic(IOptionsSnapshot<ServerOptions> serverOptions, IProcessWrapper processLogic, IIPWrapper ipLogic, ILogger<ServerLogic> logger)
        {
            if (serverOptions.Value.Servers == null)
            {
                throw new Exception("server config is null!");
            }
            _servers = serverOptions.Value.Servers;

            _processLogic = processLogic;
            _ipLogic = ipLogic;
            _logger = logger;
        }

        public ServerModel GetServer(string serverName)
        {
            ServerModel server = null;
            var serverNameArray = serverName.Split(' ');
            foreach (var namePart in serverNameArray)
            {
                if (!string.IsNullOrWhiteSpace(namePart))
                {
                    server = _servers.FirstOrDefault(x => x.Name.ToLower().Contains(namePart.ToLower()));
                }
                if (server != null)
                {
                    break;
                }
            }
            return server;
        }

        public StartServerResult StartServer(ServerModel server)
        {
            StartServerResult result;
            if (!IsServerOnline(server))
            {
                RunProgram(server.Start, server.StartArguments);

                result = StartServerResult.Success;
            }
            else
            {
                result = StartServerResult.AlreadyOnline;
            }
            return result;
        }

        public StopServerResult StopServer(ServerModel server)
        {
            var serverProcess = GetServerProcess(server);

            StopServerResult stopServerResult;
            if (serverProcess != null)
            {
                var result = "";
                try
                {
                    _processLogic.StopProcess(serverProcess);
                }
                catch (Exception ex)
                {
                    result = $"{ex.Message} Process Id: {serverProcess.Id}";
                }

                if (result == string.Empty)
                {
                    stopServerResult = new StopServerResult(StopServerResultType.Success, string.Empty);
                }
                else
                {
                    stopServerResult = new StopServerResult(StopServerResultType.StopProcessFailed, result);
                }
            }
            else
            {
                stopServerResult = new StopServerResult(StopServerResultType.ProcessNotFound, string.Empty);
            }

            return stopServerResult;
        }

        public bool IsServerOnline(ServerModel server)
        {
            var serverStatus = GetServerStatus(server);
            var online = serverStatus == ServerStatus.Online || serverStatus == ServerStatus.Starting;
            return online;
        }

        public ServerStatus GetServerStatus(ServerModel server)
        {
            bool running = false;
            var process = GetServerProcess(server);

            if (process != null)
            {
                try
                {
                    if (process != null && !process.HasExited)
                    {
                        running = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to find server's process");
                    //couldn't find the process, so we're just going to keep plugging along
                }
            }

            var listening = _ipLogic.IsPortInUse(server.Port);
            ServerStatus status;
            if (running && listening)
            {
                status = ServerStatus.Online;
            }
            else if (listening)
            {
                status = ServerStatus.Online;
            }
            else if (running)
            {
                status = ServerStatus.Starting;
            }
            else
            {
                status = ServerStatus.Offline;
            }
            return status;
        }

        public bool UpdateServer(ServerModel server, ISocketMessageChannel channel)
        {
            var result = false;
            var isOnline = IsServerOnline(server);
            if (!isOnline)
            {
                channel.SendMessageAsync($"Attempting to update {server.Name}");
                try
                {
                    var updateProcess = RunProgram(server.Update, server.UpdateArguments);
                    updateProcess.WaitForExit(1800000);
                    if (updateProcess.HasExited)
                    {
                        result = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update server");
                }
            }
            return result;
        }

        public Process GetServerProcess(ServerModel server)
        {
            Process process;
            try
            {
                process = GetProcessForFilePath(server.Start);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Failed to get server process");
                process = null;
            }

            return process;
        }

        public Process GetProcessForFilePath(string filePath)
        {
            var processes = _processLogic.GetProcesses();
            Process result = null;
            var matchingProcesses = new List<Process>();
            foreach (var process in processes)
            {
                try
                {
                    var fileName = process.MainModule.FileName;
                    if (fileName == filePath)
                    {
                        matchingProcesses.Add(process);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Failed to access process {process.Id}", ex);
                }
            }

            if (matchingProcesses.Count <= 0)
            {
                _logger.LogError("Failed to get process for filepath");
            }
            else if (matchingProcesses.Count > 1)
            {
                throw new Exception("Multiple processes found for filepath!");
            }
            else
            {
                result = matchingProcesses.First();
            }

            if (result == null)
            {
                throw new Exception("Could not find process");
            }
            return result;
        }

        public Process RunProgram(string filePath, string arguments)
        {
            var startInfo = new ProcessStartInfo(filePath, arguments)
            {
                WorkingDirectory = GetWorkingDirectoryFromFilePath(filePath),
                WindowStyle = ProcessWindowStyle.Normal,
                CreateNoWindow = false,
                UseShellExecute = true
            };
            return _processLogic.StartProcess(startInfo);
        }

        public static string GetWorkingDirectoryFromFilePath(string filePath)
        {
            int lastSlashIndex = filePath.LastIndexOf('\\');
            string workingDirectory = filePath.Substring(0, lastSlashIndex);
            return workingDirectory;
        }

        public List<ServerModel> GetServers()
        {
            return _servers;
        }
    }
}
