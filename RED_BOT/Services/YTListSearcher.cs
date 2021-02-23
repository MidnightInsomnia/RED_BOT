using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using RED_BOT.Entities;

namespace RED_BOT.Services
{
    public class YTListSearcher
    {
        private readonly Config _config;
        public YTListSearcher(Config config)
        {
            _config = config;
        }

        public async Task<string> Search(string request)
        {
            try
            {
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = _config.GoogleApiKey,
                    ApplicationName = this.GetType().ToString()
                });

                var searchListRequest = youtubeService.Search.List("snippet");
                

                searchListRequest.Q = request; // Replace with your search term.
                searchListRequest.MaxResults = 5;
                searchListRequest.Type = "playlist";
                
                // Call the search.list method to retrieve results matching the specified query term.
                var searchListResponse = await searchListRequest.ExecuteAsync();

                var result = searchListResponse.Items[0].Id;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"РЕЗУЛЬТАТ: {result.PlaylistId}");
                Console.ForegroundColor = ConsoleColor.White;

                return String.Format("https://youtube.com/playlist?list={0}", result.PlaylistId);
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }

            return String.Empty;
        }
    }
}
