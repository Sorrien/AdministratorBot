using AdministratorBot.Admin;
using AdministratorBot.Logic.Models.Server;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace AdministratorBot.Logic
{
    public interface IAdminBotLogic
    {
        string RunCommand(string command, SocketMessage message);
    }
    public class AdminBotLogic : IAdminBotLogic
    {
        private readonly IServerLogic _serverLogic;
        private readonly IConfiguration _configuration;
        public AdminBotLogic(IServerLogic serverLogic, IConfiguration configuration) => (_serverLogic, _configuration) = (serverLogic, configuration);

        public string RunCommand(string command, SocketMessage message)
        {
            var response = "";
            var commandWord = "";
            var commandArray = command.Split(' ');
            if (commandArray.Length > 0)
            {
                commandWord = commandArray[0].ToLower();
            }
            var commandParameters = command[commandWord.Length..];
            ServerModel server;
            var parameterError = "This command requires parameters.";
            var author = message.Author;
            var authorMention = author.Mention;
            var isAdmin = IsUserAdmin(author.Username, author.Discriminator);
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
                        server = _serverLogic.GetServer(commandParameters);
                        if (server == null)
                        {
                            response = $"I can't find \"{commandParameters}\"";
                        }
                        else
                        {
                            var serverStatus = _serverLogic.GetServerStatus(server);
                            response = $"{server.Name} is {serverStatus}!";
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
                        server = _serverLogic.GetServer(commandParameters);
                        if (server == null)
                        {
                            response = $"I can't find \"{commandParameters}\"";
                        }
                        else
                        {
                            var startResult = _serverLogic.StartServer(server);
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
                    if (isAdmin)
                    {
                        if (string.IsNullOrWhiteSpace(commandParameters))
                        {
                            response = parameterError;
                        }
                        else
                        {
                            server = _serverLogic.GetServer(commandParameters);
                            if (server == null)
                            {
                                response = $"I can't find \"{commandParameters}\"";
                            }
                            else
                            {
                                bool isOnline = _serverLogic.IsServerOnline(server);
                                if (isOnline)
                                {
                                    response = $"{server.Name} cannot be updated while it is running";
                                }
                                else
                                {
                                    bool updateSucceeded = _serverLogic.UpdateServer(server, message.Channel);
                                    if (updateSucceeded)
                                    {
                                        response = $"Update for {server.Name} completed";
                                    }
                                    else
                                    {
                                        response = $"Update for {server.Name} failed or timedout";
                                    }
                                }
                            }
                        }
                    }
                    break;
                case "stop":
                case "kill":
                    if (isAdmin)
                    {
                        if (string.IsNullOrWhiteSpace(commandParameters))
                        {
                            response = parameterError;
                        }
                        else
                        {
                            server = _serverLogic.GetServer(commandParameters);
                            var stopServerResult = _serverLogic.StopServer(server);
                            switch (stopServerResult.ResultType)
                            {
                                case StopServerResultType.ProcessNotFound:
                                    response = "I couldn't find the process for the server.";
                                    break;
                                case StopServerResultType.Success:
                                    response = "Success";
                                    break;
                                case StopServerResultType.StopProcessFailed:
                                    response = $"Failed due to: {stopServerResult.ResultMessage}";
                                    break;
                            }
                        }
                    }
                    break;
                case "servers":
                    response = "Server List: ";
                    var servers = _serverLogic.GetServers();
                    foreach (var s in servers)
                    {
                        var serverStatus = _serverLogic.GetServerStatus(s);
                        response += $"\n{s.Name}: {serverStatus}\nAddress: {s.Address}\nPort: {s.Port}";
                    }
                    break;
                case "help":
                    var commandModels = new List<CommandModel>();
                    _configuration.GetSection("commands").Bind(commandModels);
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
                        var commandName = _configuration[sharedKey + "Command"];
                        nullCommand = commandName == null;
                        if (nullCommand)
                        {
                            response = authorMention + " I do not know this command.";
                            break;
                        }
                        else if (command == commandName)
                        {
                            response = authorMention + " " + _configuration[sharedKey + "Response"];
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

        public bool IsUserAdmin(string username, string discriminator)
        {
            var authorUniqueName = $"{username}{discriminator}";
            var administrators = new List<string>();
            _configuration.GetSection("administrators").Bind(administrators);
            return administrators.Contains(authorUniqueName);
        }
    }
}
