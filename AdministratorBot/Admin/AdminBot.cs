using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.IO;
using Discord.WebSocket;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

namespace AdministratorBot.Admin
{
    public class AdminBot
    {
        public enum StartServerResult
        {
            Success,
            AlreadyOnline,
            AlreadyInQueue,
            Other
        }

        public static IConfigurationRoot Configuration { get; set; }
        public List<ServerModel> Servers
        {
            get
            {
                var value = new List<ServerModel>();
                Configuration.GetSection("Servers").Bind(value);
                return value;
            }
        }
        public string CommandPrefix
        {
            get
            {
                return Configuration["options:prefix"];
            }
        }
        public Dictionary<int, string> CurrentlyRunningServers { get; set; } //process id for the server name
        public AdminBot()
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("commands.json", false, true)
             .AddJsonFile("servers.json", false, true);

            Configuration = builder.Build();
            CurrentlyRunningServers = new Dictionary<int, string>();
        }

        public void Startup()
        {
            foreach(var server in Servers)
            {
                if(!CurrentlyRunningServers.Any(x => x.Value == server.Name))
                {
                    try
                    {
                        var process = GetProcessForFilePath(server.Start);
                        CurrentlyRunningServers.Add(process.Id, server.Name);                      
                    }
                    catch(Exception ex)
                    {

                    }
                }
            }
        }

        public async Task<string> RunCommand(string command, SocketMessage message)
        {
            var response = "";
            var commandWord = "";
            var commandArray = command.Split(' ');
            if (commandArray.Length > 0)
            {
                commandWord = commandArray[0].ToLower();
            }
            var commandParameters = command.Substring(commandWord.Length, command.Length - commandWord.Length);
            ServerModel server;
            var parameterError = "This command requires parameters.";
            var author = message.Author;
            var authorMention = author.Mention;
            var IsAdmin = author.Username + author.Discriminator == "Sorrien9243";
            switch (commandWord)
            {
                case "":
                    response = "I didn't hear a command in there.";
                    break;
                case "status":
                    if (string.IsNullOrWhiteSpace(commandParameters))
                    {
                        response = parameterError;
                    }
                    else
                    {
                        server = GetServer(commandParameters);
                        if (server == null)
                        {
                            response = $"I can't find \"{commandParameters}\"";
                        }
                        else
                        {
                            response = $"{server.Name} is {ServerStatus(server)}!";
                        }
                    }
                    break;
                case "restart":
                case "start":
                    if (string.IsNullOrWhiteSpace(commandParameters))
                    {
                        response = parameterError;
                    }
                    else
                    {
                        server = GetServer(commandParameters);
                        if (server == null)
                        {
                            response = $"I can't find \"{commandParameters}\"";
                        }
                        else
                        {
                            var serverCommandState = new ServerActionModel
                            {
                                Server = server,
                                Channel = message.Channel
                            };
                            var startResult = StartServer(serverCommandState);
                            var reasonPhrase = "";
                            switch (startResult)
                            {
                                case StartServerResult.Success:
                                    response = $"Starting up the {server.Name} server!";
                                    break;
                                case StartServerResult.AlreadyOnline:
                                    reasonPhrase = $"The {server.Name} server is already online.";
                                    break;
                                case StartServerResult.AlreadyInQueue:
                                    reasonPhrase = $"The {server.Name} server is already in the process of starting up.";
                                    break;
                                case StartServerResult.Other:
                                    reasonPhrase = $"I have no idea why {server.Name} is not starting.";
                                    break;
                            }
                            if (startResult != StartServerResult.Success)
                            {
                                response = $"I'm afraid I can't do that, {authorMention}. {reasonPhrase}";
                            }
                        }
                    }
                    break;
                case "update":
                    if (IsAdmin)
                    {
                        if (string.IsNullOrWhiteSpace(commandParameters))
                        {
                            response = parameterError;
                        }
                        else
                        {
                            server = GetServer(commandParameters);
                            if (server == null)
                            {
                                response = $"I can't find \"{commandParameters}\"";
                            }
                            else
                            {
                                if (UpdateServer(server, message.Channel))
                                {
                                    //response = $"Updating server {server.Name}, this could take a while.";
                                    response = $"Update for {server.Name} completed";
                                }
                                else
                                {
                                    response = $"Update for {server.Name} failed or timedout";
                                }
                            }
                        }
                    }
                    break;
                case "stop":
                case "kill":
                    if (IsAdmin)
                    {
                        if (string.IsNullOrWhiteSpace(commandParameters))
                        {
                            response = parameterError;
                        }
                        else
                        {
                            server = GetServer(commandParameters);
                            var serverProcessId = GetServerProcessId(server);
                            if (serverProcessId != -1)
                            {
                                var result = StopProcess(serverProcessId);
                                if (result == string.Empty)
                                {
                                    response = "Success";
                                }
                                else
                                {
                                    response = "Failed due to: " + result;
                                }
                            }
                            else
                            {
                                response = "I couldn't find the process for the server.";
                            }
                        }
                    }
                    break;
                case "servers":
                    response = "Server List: ";
                    foreach (var x in Servers)
                    {
                        response += $"\n{x.Name}: {ServerStatus(x)}\nAddress: {x.Address}\nPort: {x.Port}";
                    }
                    break;
                case "help":
                    var commandModels = new List<CommandModel>();
                    Configuration.GetSection("commands").Bind(commandModels);
                    response += "Commands: ";
                    foreach (var x in commandModels)
                    {
                        response += $"\n{x.Command}";
                    }
                    response += "\nstatus (server name)\nrestart (server name)\nservers";
                    break;
                default:
                    int commandIndex = 0;
                    bool nullCommand = false;
                    while (!nullCommand)
                    {
                        var sharedKey = "commands:" + commandIndex + ":";
                        var commandName = Configuration[sharedKey + "Command"];
                        nullCommand = commandName == null;
                        if (nullCommand)
                        {
                            response = authorMention + " I do not know this command.";
                            break;
                        }
                        else if (command == commandName)
                        {
                            response = authorMention + " " + Configuration[sharedKey + "Response"];

                            break;
                        }
                        else
                        {
                            commandIndex++;
                        }
                    }
                    break;
            }
            return response;
        }


        private ServerModel GetServer(string serverName)
        {
            ServerModel server = null;
            var serverNameArray = serverName.Split(' ');
            foreach (var namePart in serverNameArray)
            {
                if (!string.IsNullOrWhiteSpace(namePart))
                {
                    server = Servers.FirstOrDefault(x => x.Name.ToLower().Contains(namePart.ToLower()));
                }
                if (server != null)
                {
                    break;
                }
            }
            return server;
        }

        private string ServerStatus(ServerModel server)
        {
            var status = "";
            var running = IsServerOnline(server);
            var listening = IsServerPortTaken(server.Port);
            if (running && listening)
            {
                status = "Online";
            }
            else
            {
                if (running)
                {
                    status = "Starting";
                }
                else
                {
                    status = "Offline";
                }
            }
            return status;
        }

        private bool IsServerPortTaken(int port)
        {
            var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().ToList();
            listeners.AddRange(IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners());
            var listener = listeners.FirstOrDefault(x => x.Port == port);
            return listeners.Any(x => x.Port == port);
        }

        private bool IsServerOnline(ServerModel server)
        {
            var online = false;
            var processId = GetServerProcessId(server);
            if (processId > 0)
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    if (process != null && !process.HasExited)
                    {
                        online = true;
                    }
                }
                catch (Exception ex)
                {
                    //couldn't find the process, so we're just going to keep plugging along
                }
            }

            if (IsServerPortTaken(server.Port))
            {
                online = true;
            }
            return online;
        }

        private StartServerResult StartServer(ServerActionModel serverActionModel)
        {
            var server = serverActionModel.Server;
            var result = StartServerResult.Other;
            if (!IsServerOnline(server))
            {
                var serverProcess = RunProgram(server.Start, server.StartArguments);

                if (server.HasLauncher) {
                    serverProcess = GetProcessForFilePath(server.Start);
                }
                serverProcess.Exited += ServerProcess_Exited;
                CurrentlyRunningServers.Add(serverProcess.Id, server.Name);

                result = StartServerResult.Success;

            }
            else
            {
                result = StartServerResult.AlreadyOnline;
            }
            return result;
        }

        private void ServerProcess_Exited(object sender, EventArgs e)
        {
            var process = (Process)sender;
            var serverName = CurrentlyRunningServers[process.Id];
            var server = Servers.FirstOrDefault(x => x.Name == serverName);
            if (CurrentlyRunningServers.Any(x => x.Key == process.Id))
            {
                CurrentlyRunningServers.Remove(process.Id);
            }
        }

        private bool UpdateServer(ServerModel server, ISocketMessageChannel channel)
        {
            var result = false;
            if (!IsServerOnline(server))
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

                }
            }
            return result;
        }

        private int GetServerProcessId(ServerModel server)
        {
            var id = -1;
            try
            {
                id = CurrentlyRunningServers.FirstOrDefault(x => x.Value == server.Name).Key;
            }
            catch (Exception ex)
            {

            }

            return id;
        }

        private Process GetProcessForFilePath(string filePath)
        {
            var processes = Process.GetProcesses();
            Process result = null;
            foreach (var process in processes)
            {
                try
                {
                    if (process.MainModule.FileName == filePath)
                    {
                        result = process;
                        break;
                    }
                }
                catch(Exception ex)
                {

                }
            }
            if(result == null)
            {
                throw new Exception("Could not find process");
            }
            return result;
        }

        private string StopProcess(int id)
        {
            var result = "";
            try
            {
                var worker = Process.GetProcessById(id);
                worker.Kill();
                worker.WaitForExit();
                worker.Dispose();
                if (CurrentlyRunningServers.Any(x => x.Key == id))
                {
                    CurrentlyRunningServers.Remove(id);
                }
            }
            catch (Exception ex)
            {
                result = $"{ex.Message} Process Id: {id}";
            }
            return result;
        }

        private Process RunBatch(string filePath)
        {
            var process = Process.Start(filePath);
            return process;
        }

        private Process RunProgram(string filePath, string arguments)
        {
            var startInfo = new ProcessStartInfo(filePath, arguments);
            startInfo.WorkingDirectory = GetWorkingDirectoryFromFilePath(filePath);
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = true;
            var process = Process.Start(startInfo);
            return process;
        }

        private string GetWorkingDirectoryFromFilePath(string filePath)
        {
            var workingDirectory = "";
            var lastSlashIndex = filePath.LastIndexOf('\\');
            workingDirectory = filePath.Substring(0, lastSlashIndex);
            return workingDirectory;
        }

        //private long IPStringToInt(string addr)
        //{
        //    // careful of sign extension: convert to uint first;
        //    // unsigned NetworkToHostOrder ought to be provided.
        //    IPAddress address = IPAddress.Parse(addr);
        //    byte[] bytes = address.GetAddressBytes();
        //    //Array.Reverse(bytes); // flip big-endian(network order) to little-endian
        //    uint intAddress = BitConverter.ToUInt32(bytes, 0);
        //    return (long)intAddress;
        //}
    }
}
