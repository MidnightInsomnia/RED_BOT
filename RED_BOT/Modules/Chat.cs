using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Victoria;

namespace RED_BOT.Modules
{
    public class Ping : ModuleBase<SocketCommandContext>
    {
        [Command("ping", RunMode = RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        public async Task Pong()
        {
            await ReplyAsync("PONG!");
        }
        [Command("clean", RunMode = RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        public async Task Clean(int counter)
        {
            await ReplyAsync("Clean!");
        }
    }
}
