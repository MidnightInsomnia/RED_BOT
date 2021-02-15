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

            var embedBuilder = new EmbedBuilder { Description = $"Сейчас играет: [{track.Title}]({track.Url})" };

            var embedMessage = embedBuilder.Build();

            await player.TextChannel.SendMessageAsync(null, false, embedMessage);
        }

        //public async Task EmbedTrackName(string trackName, string url)
        //{
        //    var embedBuilder = new EmbedBuilder { Description = $"Сейчас играет: [{trackName}]({url})" };
        //    //embedBuilder.AddField("", $"Сейчас играет: [{trackName}]({url})");            

        //    var embedMessage = embedBuilder.Build();
        //    await ReplyAsync(embed: embedMessage);
        //}

        public async Task<string> PlayAsync(string query, IGuild guildId)
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

            //var res = await _lavaNode.SearchAsync(query);


            if(searchResponse.LoadStatus == LoadStatus.NoMatches || searchResponse.LoadStatus == LoadStatus.LoadFailed)
            {
                return "Совпадения не найдены.";
            }

            IReadOnlyList<LavaTrack> tracks = searchResponse.Tracks;

            if (!(_player.PlayerState == PlayerState.Playing))
            {
                if (query.Contains("https://youtube.com/playlist?list="))
                {
                    foreach (var song in tracks)
                    {
                        _player.Queue.Enqueue(song);
                    }
                    //await _player.PlayAsync(tracks[0]);
                    //return $"Сейчас играет: {tracks[0].Title}";
                    return $"Добавлено {tracks.Count} треков";
                }
                else
                {
                    _player.Queue.TryDequeue(out var item);
                    await _player.PlayAsync(item);
                    //_player.Queue.Enqueue(tracks[0]);
                    //Sawait _player.PlayAsync(tracks[0]);
                    return $"Трек {tracks[0].Title} добавлен в очередь.";
                }
            }
            else
            {
                _player.Queue.TryDequeue(out var item);
                await _player.PlayAsync(item);
                //_player.Queue.Enqueue(tracks[0]);
                //await _player.PlayAsync(tracks[0]);
            }
            return "А?";
        }

        public async Task<string> SkipAsync(IGuild guildId)
        {
            var _player = _lavaNode.GetPlayer(guildId);
            if (_player is null || _player.Queue.Count is 0)
                return "Плейлист пуст.";

            var oldTrack = _player.Track;
            await _player.SkipAsync();
            return $"Пропущен трек: {oldTrack.Title}";
        }

        public async Task<string> SetVolumeAsync(ushort vol, IGuild guildId)
        {
            var _player = _lavaNode.GetPlayer(guildId);
            if (_player is null)
                return "Плеер ничего не проигрывает.";

            if (vol > 1000 || vol <= 2)
            {
                return "Уровень звука можно настраивать в диапазоне 2 - 1000 (рекомендуется не более 150, выше - для любителей BASSBOOSTED)";
            }

            await _player.UpdateVolumeAsync(vol);
            return $"Уровень звука равен: {vol}";
        }

        public async Task<string> StopAsync(IGuild guildId)
        {
            var _player = _lavaNode.GetPlayer(guildId);
            if (_player is null)
                return "Ошибка воспроизведения";
            await _player.StopAsync();
            return "Воспроизведение остановлено.";
        }

        public async Task<string> PauseOrResumeAsync(IGuild guildId)
        {
            var _player = _lavaNode.GetPlayer(guildId);
            if (_player is null)
                return "Плеер ничего не проигрывает.";

            if (!(_player.PlayerState == PlayerState.Paused))
            {
                await _player.PauseAsync();
                return "Пауза.";
            }
            else
            {
                await _player.ResumeAsync();
                return "Воспроизведение.";
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
                await player.TextChannel.SendMessageAsync("Плейлист закончился.");
                return;
            }
            await player.PlayAsync(nextTrack);
        }

        public async Task<string> JoinAsync(SocketGuildUser user, ITextChannel textChannel)
        {
            if (user.VoiceChannel is null)
            {
               return "Подключитесь к голосовому каналу";
            }
            else
            {
                await _lavaNode.JoinAsync(user.VoiceChannel, textChannel);
                return $"Бот зашёл в {user.VoiceChannel.Name}";
            }
        }

        public async Task<string> LeaveAsync(SocketGuildUser user, IGuild guildId)
        {
            var _player = _lavaNode.GetPlayer(guildId);

            if (user.VoiceChannel is null)
            {
                return "Подключитесь к голосовому каналу, чтобы отключить бота.";
            }
            else if (!(_player.PlayerState == PlayerState.Connected))
            {
                await _lavaNode.LeaveAsync(user.VoiceChannel);
                return "Бот не подключен ни к одному каналу.";
            }
            //await _lavaNode.LeaveAsync(user.VoiceChannel);
            return $"Бот покинул {user.VoiceChannel.Name}";
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
