using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RED_BOT.Entities;
using RED_BOT.Services;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace RED_BOT
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _cmdService;
        private readonly IServiceProvider _services;
        private readonly Config _config;

        public CommandHandler(DiscordSocketClient client, CommandService cmdService, IServiceProvider services, Config config)
        {
            _client = client;
            _cmdService = cmdService;
            _services = services;
            _config = config;
        }

        public async Task InitializeAsync()
        {
            await _cmdService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _cmdService.Log += LogAsync;
            _client.MessageReceived += HandleMessageAsync;
        }

        private async Task HandleMessageAsync(SocketMessage socketMessage)
        {
            var argPos = 0;
            if (socketMessage.Author.IsBot) return;

            var userMessage = socketMessage as SocketUserMessage;

            if (userMessage == null)
                return;

            if (!(userMessage.HasMentionPrefix(_client.CurrentUser, ref argPos)
                || userMessage.HasStringPrefix(_config.Prefix, ref argPos)))
                return;
            
            var context = new SocketCommandContext(_client, userMessage);
            var result = await _cmdService.ExecuteAsync(context, argPos, _services);
        }

        private Task LogAsync(LogMessage logMessage)
        {
            Console.WriteLine(logMessage.Message);
            return Task.CompletedTask;
        }
    }
}
