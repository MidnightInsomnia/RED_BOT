using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Rest;

namespace RED_BOT.Services
{
    public class MusicService
    {
        private readonly LavaNode _lavaNode;
        private readonly DiscordSocketClient _client;
        private readonly LogService _logService;

        public MusicService(LavaNode lavaNode, DiscordSocketClient client, LogService logService)
        {
            _client = client;
            _lavaNode = lavaNode;
            _logService = logService;
        } 

        public Task InitializeAsync()
        {
            _client.Ready += OnReadyAsync;
            _lavaNode.OnLog += LogAsync;
            _lavaNode.OnTrackStuck += TrackStucked;
            _lavaNode.OnTrackStarted += NowPlayingAsync;
            _lavaNode.OnTrackEnded += TrackEnded;
            
            return Task.CompletedTask;
        }

        private Task TrackStucked(TrackStuckEventArgs arg)
        {
            Console.WriteLine("STUCKED");
            return Task.CompletedTask;
        }

        private async Task NowPlayingAsync(TrackStartEventArgs arg)
        {
            var player = arg.Player;
            var track = arg.Track;

            await SendEmbed($"Сейчас играет: [{track.Title}]({track.Url})", player.TextChannel);
        }

        private async Task SendEmbed(string message, ITextChannel channel)
        {
            EmbedBuilder embedBuilder = null;

            embedBuilder = new EmbedBuilder { Description = message };
            var embedMessage = embedBuilder.Build();
            await channel.SendMessageAsync(null, false, embedMessage);
        }

        public async Task PlayAsync(string query, IGuild guildId)
        {
            var _player = _lavaNode.GetPlayer(guildId);

            SearchResponse searchResponse;

            if (query.Contains("https://youtube.com/playlist?list="))
            {
                searchResponse = await _lavaNode.SearchAsync(query);
            }
            else
            {
                searchResponse = await _lavaNode.SearchYouTubeAsync(query);
            }                

            if(searchResponse.LoadStatus == LoadStatus.NoMatches || searchResponse.LoadStatus == LoadStatus.LoadFailed)
            {
                await SendEmbed("Совпадения не найдены.", _player.TextChannel);
            }
            else
            {
                IReadOnlyList<LavaTrack> tracks = searchResponse.Tracks;

                if (query.Contains("https://youtube.com/playlist?list="))
                {
                    foreach (var song in tracks)
                    {
                        _player.Queue.Enqueue(song);
                    }
                    await SendEmbed($"Добавлено {tracks.Count} треков", _player.TextChannel);
                }
                else
                {
                    if (_player.PlayerState == PlayerState.Playing || _player.PlayerState == PlayerState.Paused)
                    {
                        await SendEmbed($"Трек {tracks[0].Title} добавлен в очередь.", _player.TextChannel);
                    }
                    _player.Queue.Enqueue(tracks[0]);
                }
                
                if (!(_player.PlayerState == PlayerState.Playing) && (!(_player.PlayerState == PlayerState.Paused)))
                {
                    _player.Queue.TryDequeue(out var item);
                    await _player.PlayAsync(item);
                }
            }
        }

        public async Task SkipAsync(IGuild guildId)
        {
            var _player = _lavaNode.GetPlayer(guildId);
            if (_player is null || _player.Queue.Count is 0)
            {
                await SendEmbed("Плейлист пуст.", _player.TextChannel);
            }

            await _player.SkipAsync();
        }

        public async Task SetVolumeAsync(ushort vol, IGuild guildId)
        {

            var _player = _lavaNode.GetPlayer(guildId);
            if (_player is null)
            {
                await SendEmbed("Плеер ничего не проигрывает.", _player.TextChannel);
            }
            else
            {
                if (vol > 1000 || vol <= 2)
                {
                    await SendEmbed("Уровень звука можно настраивать в диапазоне 2 - 1000 (рекомендуется не более 150, выше - для любителей BASSBOOSTED)", _player.TextChannel);
                }

                await _player.UpdateVolumeAsync(vol);
                await SendEmbed($"Уровень звука равен: {vol}", _player.TextChannel);
            }
        }

        public async Task StopAsync(IGuild guildId)
        {
            var _player = _lavaNode.GetPlayer(guildId);
            if (_player is null)
            {
                await SendEmbed("Ошибка воспроизведения", _player.TextChannel);
            }
            else
            {
                _player.Queue.Clear();
                await _player.StopAsync();
                await SendEmbed("Воспроизведение остановлено.", _player.TextChannel);
            }
        }

        public async Task PauseOrResumeAsync(IGuild guildId)
        {

            var _player = _lavaNode.GetPlayer(guildId);
            if (_player is null)
            {
                await SendEmbed("Плеер ничего не проигрывает.", _player.TextChannel);
            }
            else
            {
                if (!(_player.PlayerState == PlayerState.Paused))
                {
                    await _player.PauseAsync();
                    await SendEmbed("Пауза.", _player.TextChannel);
                }
                else
                {
                    await _player.ResumeAsync();
                    await SendEmbed("Воспроизведение.", _player.TextChannel);
                }
            }
        }

        private async Task TrackEnded(TrackEndedEventArgs arg)
        {
            var player = arg.Player;
            var reason = arg.Reason;

            if (!reason.ShouldPlayNext())
                return;

            if (!player.Queue.TryDequeue(out var item) || !(item is LavaTrack nextTrack))
            {
                await SendEmbed("Плейлист закончился.", player.TextChannel);
                return;
            }
            await player.PlayAsync(nextTrack);
        }

        public async Task JoinAsync(SocketGuildUser user, ITextChannel textChannel)
        {
            if (user.VoiceChannel is null)
            {
                await SendEmbed("Подключитесь к голосовому каналу", textChannel);
            }
            else
            {
                await _lavaNode.JoinAsync(user.VoiceChannel, textChannel);
                await SendEmbed($"Бот зашёл в {user.VoiceChannel.Name}", textChannel);
            }
        }

        public async Task LeaveAsync(SocketGuildUser user, IGuild guildId)
        {
            var _player = _lavaNode.GetPlayer(guildId);

            if (user.VoiceChannel is null)
            {
                await SendEmbed("Подключитесь к голосовому каналу, чтобы отключить бота.", _player.TextChannel);
                return;
            }
            else if (!(_player.PlayerState == PlayerState.Connected))
            {
                await SendEmbed("Бот не подключен ни к одному каналу.", _player.TextChannel);
                return;
            }
            await _lavaNode.LeaveAsync(user.VoiceChannel);
            await SendEmbed($"Бот покинул {user.VoiceChannel.Name}", _player.TextChannel);
        }

        private async Task OnReadyAsync()
        {
            // Если вылетит исключение - значит WebSocket уже подключен
            if (!_lavaNode.IsConnected)
            {
                await _lavaNode.ConnectAsync();
            }
        }

        private async Task LogAsync(LogMessage logMessage)
        {
            await _logService.LogAsync(logMessage);
        }
    }
}
