using System;
using System.Threading.Tasks;

namespace RED_BOT
{
    class Program
    {
        static async Task Main(string[] args)
            => await new RedBotClient().InitializeAsync();
    }
}
