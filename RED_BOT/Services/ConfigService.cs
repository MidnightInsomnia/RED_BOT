using Newtonsoft.Json;
using RED_BOT.Entities;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RED_BOT.Services
{
    public class ConfigService
    {
        private Config config;

        //Инициализация
        public ConfigService()
        {
            config = new Config()
            {
                Token = "",
                Prefix = ""
            };
        }

        public Config GetConfig()
        {
            //Поиск файла конфигурации
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json").Replace(@"\", @"\\");
            Console.WriteLine(configPath);

            //Создание файла конфигурации в случае его отсутствия
            if (!File.Exists(configPath))
            {
                using (StreamWriter sw = File.AppendText(configPath))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(config));
                }

                Console.WriteLine("WARNING! New Config initialiazed! Need to fill in values before running commands!");
                throw new Exception("NO CONFIG AVAILABLE! Go to executable path and fill out newly created file!");
            }

            
            var data = File.ReadAllText(configPath);
            config = JsonConvert.DeserializeObject<Config>(data);

            return config;
        }
    }
}
