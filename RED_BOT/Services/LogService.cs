using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RED_BOT.Services
{
    public class LogService
    {
        private readonly SemaphoreSlim _semaphoreSlim;

        public LogService()
        {
            _semaphoreSlim = new SemaphoreSlim(1);
        }

        internal async Task LogAsync(LogMessage arg)
        {
            await _semaphoreSlim.WaitAsync();

            var timeStamp = DateTime.UtcNow.ToString("MM/dd/yyyy hh:mm tt");
            const string format = "{0,-10} {1,10}";

            Console.WriteLine($"[{timeStamp}] {string.Format(format, arg.Source, $": {arg.Message}")}");

            _semaphoreSlim.Release();
        }
    }
}
