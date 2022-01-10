using AdministratorBot.Admin;
using AdministratorBot.Logic.Models.Server;
using AdministratorBot.Settings;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdministratorBot.Logic
{
    public interface IAdminBotLogic
    {
        string RunCommand(string command, SocketMessage message);
    }
    public class AdminBotLogic : IAdminBotLogic
    {
        private readonly IServerLogic _serverLogic;
        private readonly List<CommandModel> _commands;
        private readonly List<string> _administrators;
        private readonly ILogger<AdminBotLogic> _logger;
        public AdminBotLogic(IServerLogic serverLogic, ILogger<AdminBotLogic> logger, IOptionsSnapshot<CommandOptions> commandOptions, IOptionsSnapshot<PermissionsOptions> permissionsOptions)
        {
            _serverLogic = serverLogic;
            _logger = logger;
            if (commandOptions.Value == null)
            {
                throw new Exception("command config is null!");
            }
            _commands = commandOptions.Value.Commands;

            if (permissionsOptions.Value == null)
            {
                throw new Exception("command config is null!");
            }
            _administrators = permissionsOptions.Value.Administrators;
        }

        public string RunCommand(string command, SocketMessage message)
        {
            string response = "";
            try
            {
                string commandWord = "";
                var commandArray = command.Split(' ');
                if (commandArray.Length > 0)
                {
                    commandWord = commandArray[0].ToLower();
                }
                string commandParameters = command[commandWord.Length..];
                ServerModel server;
                string parameterError = "This command requires parameters.";
                var author = message.Author;
                string authorMention = author.Mention;
                bool isAdmin = IsUserAdmin(author.Username, author.Discriminator);
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
                                string reasonPhrase = "";
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
                        response += "Commands: ";
                        foreach (var x in _commands)
                        {
                            response += $"\n{x.Command}";
                        }
                        response += "\nstatus (server name)\nrestart (server name)\nservers";
                        break;
                    default:
                        var commandModel = _commands.FirstOrDefault(x => x.Command.ToLower() == command.ToLower());
                        if (commandModel == null)
                        {
                            response = authorMention + " I do not know this command.";
                        }
                        else
                        {
                            response = authorMention + " " + commandModel.Response;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during formulation of response.");
                response = "I think I have a screw loose. Something went wrong.";
            }
            return response;
        }

        public bool IsUserAdmin(string username, string discriminator)
        {
            string authorUniqueName = $"{username}{discriminator}";
            return _administrators.Contains(authorUniqueName);
        }
    }
}
