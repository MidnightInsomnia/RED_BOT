using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RED_BOT.Entities;
using RED_BOT.Services;
using System;
using System.Threading.Tasks;
using Victoria;


namespace RED_BOT
{
    public class RedBotClient
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _cmdService;
        private IServiceProvider _services;
        private readonly LogService _logService;
        private readonly ConfigService _configService;
        private readonly Config _config;
        private readonly YTListSearcher _ytListSearcher;

        public RedBotClient()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50,
                LogLevel = LogSeverity.Debug
            });

            _cmdService = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                CaseSensitiveCommands = false
            });

            _logService = new LogService();
            _configService = new ConfigService();
            _config = _configService.GetConfig();
            _ytListSearcher = new YTListSearcher(_config);
        }

        public async Task InitializeAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();
            _client.Log += LogAsync;
            _services = SetupServices();
            
            var cmdHandler = new CommandHandler(_client, _cmdService, _services, _config);
            await cmdHandler.InitializeAsync();

            await _services.GetRequiredService<MusicService>().InitializeAsync();
            await Task.Delay(-1);
        }

        private async Task LogAsync(LogMessage logMessage)
        {
            await _logService.LogAsync(logMessage);
        }

        private IServiceProvider SetupServices()
            => new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_cmdService)
            .AddSingleton(_logService)
            .AddSingleton(_ytListSearcher)
            .AddSingleton<LavaNode>()
            .AddSingleton<LavaConfig>()
            .AddSingleton<MusicService>()
            .AddLavaNode(x => {
                x.SelfDeaf = false;
            })
            .BuildServiceProvider();
    }
}
