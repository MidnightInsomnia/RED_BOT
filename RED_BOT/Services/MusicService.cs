using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Payloads;
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

        public async Task PlayAsync(string query, IGuild guildId, SocketGuildUser user, ITextChannel channel)
        {
            LavaPlayer _player = null;

            try
            {
                _player = _lavaNode.GetPlayer(guildId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await JoinAsync(user, channel);
                try
                {
                    _player = _lavaNode.GetPlayer(guildId);
                }
                catch (Exception exp)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            
            if (_player == null)
                return;

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
                    await SendEmbed($"Добавлено {tracks.Count} треков. [<@{user.Id}>]", _player.TextChannel);
                }
                else
                {
                    if (_player.PlayerState == PlayerState.Playing || _player.PlayerState == PlayerState.Paused)
                    {
                        await SendEmbed($"Трек [{tracks[0].Title}]({tracks[0].Url}) добавлен в очередь. [<@{user.Id}>]", _player.TextChannel);
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

        public async Task SetEqualizer(int band, double gain, IGuild guildId)
        {
            //Добавить диапазоны
            var _player = _lavaNode.GetPlayer(guildId);
            if (_player is null)
            {
                await SendEmbed("Плеер ничего не проигрывает.", _player.TextChannel);
            }
            else
            {
                await SendEmbed("15 диапазонов (0 - 14) со значениями от -0.25 до 1.0", _player.TextChannel);
                EqualizerBand equalizer = new EqualizerBand(band, gain);
                await _player.EqualizerAsync(equalizer);
            }
        }

        //public async Task Seek(int hours, int minutes, int seconds, IGuild guildId)
        //{
        //    var _player = _lavaNode.GetPlayer(guildId);
        //    if (_player is null)
        //    {
        //        await SendEmbed("Плеер ничего не проигрывает.", _player.TextChannel);
        //    }
        //    else
        //    {
        //        TimeSpan s = new TimeSpan(hours, minutes, seconds);
        //        await _player.SeekAsync(s);
        //    }
        //}

        public async Task Seek(string timeCode, IGuild guildId)
        {
            var _player = _lavaNode.GetPlayer(guildId);
            if (_player is null)
            {
                await SendEmbed("Плеер ничего не проигрывает.", _player.TextChannel);
            }
            else
            {
                if (!timeCode.Contains(':'))
                {
                    await SendEmbed("Введите время в формате ЧЧ:ММ:СС", _player.TextChannel);
                    return;
                }
                var time = await timeParser(timeCode);
                if (time > _player.Track.Duration || time < _player.Track.Duration)
                {
                    await SendEmbed("Указанная временая метка не соответствует продолжительности трека.", _player.TextChannel);
                }
                else
                {
                    await _player.SeekAsync(time);
                }
            }
        }

        private async Task<TimeSpan> timeParser(string timeCode)
        {
            TimeSpan result = new TimeSpan();

            int hours;
            int minutes;
            int seconds;
            int miliseconds;

            string[] subStrings = timeCode.Split(':');
            switch(subStrings.Length)
            {
                case 0:
                    break;
                case 2:
                    minutes = int.Parse(subStrings[0]);
                    seconds = int.Parse(subStrings[1]);
                    result = new TimeSpan(0, minutes, seconds);
                    break;
                case 3:
                    hours = int.Parse(subStrings[0]);
                    minutes = int.Parse(subStrings[1]);
                    seconds = int.Parse(subStrings[2]);
                    result = new TimeSpan(hours, minutes, seconds);
                    break;
                case 4:
                    hours = int.Parse(subStrings[0]);
                    minutes = int.Parse(subStrings[1]);
                    seconds = int.Parse(subStrings[2]);
                    miliseconds = int.Parse(subStrings[3]);
                    result = new TimeSpan(0, hours, minutes, seconds, miliseconds);
                    break;
                default:
                    break;
            }

            return result;
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

        public async Task LeaveAsync(SocketGuildUser user, IGuild guildId, ITextChannel channel)
        {
            LavaPlayer _player = null;
            try
            {
                _player = _lavaNode.GetPlayer(guildId);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if(_player == null)
            {
                await SendEmbed("Бот не подключен ни к одному каналу", channel);
                return;
            }

            if (user.VoiceChannel is null)
            {
                await SendEmbed("Подключитесь к голосовому каналу, чтобы отключить бота.", _player.TextChannel);
                return;
            }

            if (_player.PlayerState == PlayerState.Disconnected)
                if (_player == null)
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
