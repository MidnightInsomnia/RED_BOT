using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
            => await ReplyAsync(await _musicService.JoinAsync(Context.User as SocketGuildUser, Context.Channel as ITextChannel));

        [Command("Leave")]
        public async Task Leave()
            => await ReplyAsync(await _musicService.LeaveAsync(Context.User as SocketGuildUser, Context.Guild));

        [Command("Play")]
        public async Task Play([Remainder]string querry)
            => await ReplyAsync(await _musicService.PlayAsync(querry, Context.Guild));

        [Command("Stop")]
        public async Task Stop()
            => await ReplyAsync(await _musicService.StopAsync(Context.Guild));

        [Command("Skip")]
        public async Task Skip()
            => await ReplyAsync(await _musicService.SkipAsync(Context.Guild));

        [Command("Volume")]
        public async Task Volume(ushort vol)
            => await ReplyAsync(await _musicService.SetVolumeAsync(vol, Context.Guild));

        [Command("Pause")]
        public async Task Pause()
            => await ReplyAsync(await _musicService.PauseOrResumeAsync(Context.Guild));

        [Command("Resume")]
        public async Task Resume()
            => await ReplyAsync(await _musicService.PauseOrResumeAsync(Context.Guild));
    }
}
