using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RED_BOT.Modules
{
    public class EmbedReply
    {
        public async Task<Embed> BuildEmbed(string message)
        {
            var embedBuilder = new EmbedBuilder { Description = message };

            var embedMessage = embedBuilder.Build();

            return embedMessage;
        }

        public async Task<Embed> BuildEmbedNowPlaying(string track, string uri)
        {
            var embedBuilder = new EmbedBuilder { Description = $"Сейчас играет: [{track}]({uri})" };

            var embedMessage = embedBuilder.Build();

            return embedMessage;
        }

    }
}
