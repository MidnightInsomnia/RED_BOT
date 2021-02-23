using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RED_BOT.Enums;
using RED_BOT.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Victoria;

namespace RED_BOT.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private MusicService _musicService;

        public Music(MusicService musicService)
            => _musicService = musicService;

        [Command("Join")]
        public async Task JoinAsync()
            => await _musicService.JoinAsync(Context.User as SocketGuildUser, Context.Channel as ITextChannel);

        [Command("Leave")]
        public async Task Leave()
            => await _musicService.LeaveAsync(Context.User as SocketGuildUser, Context.Guild, Context.Channel as ITextChannel);

        [Command("Play")]
        public async Task Play([Remainder] string querry)
            => await _musicService.PlayAsync(querry, Context.Guild, Context.User as SocketGuildUser, Context.Channel as ITextChannel, SearchMode.YTSongSearch);

        [Command("YTList")]
        public async Task Playlist([Remainder] string querry)
            => await _musicService.PlayAsync(querry, Context.Guild, Context.User as SocketGuildUser, Context.Channel as ITextChannel, SearchMode.YTListSearch);

        [Command("PlayCloud")]
        public async Task PlayCloud([Remainder] string querry)
            => await _musicService.PlayAsync(querry, Context.Guild, Context.User as SocketGuildUser, Context.Channel as ITextChannel, SearchMode.CloudSongSearch);

        [Command("CloudList")]
        public async Task CloudList([Remainder] string querry)
            => await _musicService.PlayAsync(querry, Context.Guild, Context.User as SocketGuildUser, Context.Channel as ITextChannel, SearchMode.CloudListSearch);

        [Command("Stop")]
        public async Task Stop()
            => await _musicService.StopAsync(Context.Guild);

        [Command("Skip")]
        public async Task Skip()
            => await _musicService.SkipAsync(Context.Guild);

        [Command("Volume")]
        public async Task Volume(ushort vol)
            => await _musicService.SetVolumeAsync(vol, Context.Guild);

        [Command("Pause")]
        public async Task Pause()
            => await _musicService.PauseOrResumeAsync(Context.Guild);

        [Command("Resume")]
        public async Task Resume()
            => await _musicService.PauseOrResumeAsync(Context.Guild);

        [Command("Equalizer")]
        public async Task Equalizer(int band, double gain)
            => await _musicService.SetEqualizer(band, gain, Context.Guild);

        //[Command("Seek")]
        //public async Task Seek(int hours, int minutes, int seconds)
        //    => await _musicService.Seek(hours, minutes, seconds, Context.Guild);

        [Command("Seek")]
        public async Task Seek(string timeCode)
            => await _musicService.Seek(timeCode, Context.Guild);
    }
}
