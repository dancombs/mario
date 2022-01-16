using System.Reflection;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading;
using MongoDB.Driver;
using MongoDB.Bson;
using Coinbase.Pro;

using Mario;
using Mario.Setup;
using Mario.Study;
using Mario.Trade;

namespace Mario
{
    class Program
    {
        public static void preflight(string env)
        {
            Global.directory_exe = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            Global.file_sound_player = Path.DirectorySeparatorChar + "usr" +
                Path.DirectorySeparatorChar + "bin" +
                Path.DirectorySeparatorChar + "afplay";

            Global.directory_sound = Global.directory_exe
                                     + Path.DirectorySeparatorChar
                                     + "x_sound"
                                     + Path.DirectorySeparatorChar;

            Global.mongodb_url = "mongodb://127.0.0.1:27017";
            Global.mongodb_client = new MongoClient(Global.mongodb_url);
            Global.mongodb_database = Global.mongodb_client.GetDatabase(Global.db_name);

            Global.collection_app_settings =
                Global.mongodb_database.GetCollection<BsonDocument>(Global.db_app_settings);

            Global.collection_trade_settings =
                Global.mongodb_database.GetCollection<BsonDocument>(Global.db_trade_settings);

            Global.collection_trade_log =
                Global.mongodb_database.GetCollection<BsonDocument>(Global.db_trade_log);

            Global.filter_empty = Builders<BsonDocument>.Filter.Empty;

            var results_lookback = Global.collection_app_settings.
                Find(Global.filter_empty).
                Single();

            switch (env)
            {
                case "STAGING":
                    Global.coinbase_client = new CoinbaseProClient(new Config
                    {
                        UseTimeApi = true,
                        ApiKey = results_lookback.GetValue("api_key").ToString(),
                        Secret = results_lookback.GetValue("secret").ToString(),
                        Passphrase = results_lookback.GetValue("passphrase").ToString(),
                        ApiUrl = "https://api-public.sandbox.pro.coinbase.com"
                    });

                    break;
                case "PRODUCTION":
                    Global.coinbase_client = new CoinbaseProClient(new Config
                    {
                        UseTimeApi = true,
                        ApiKey = results_lookback.GetValue("api_key").ToString(),
                        Secret = results_lookback.GetValue("secret").ToString(),
                        Passphrase = results_lookback.GetValue("passphrase").ToString(),
                    });
                    break;
                default:
                    Console.WriteLine("ERROR...ENVIRONMENT STRING INVALID OR NOT FOUND.");
                    break;
            }
        }

        static void Main(string[] args)
        {
            var command = args[0];

            switch (command)
            {
                case "test":
                    preflight(args[1]);
                    Console.WriteLine(Global.file_sound_player);
                    Util.trade_alarm("Sell");
                    break;
                case "run":
                    preflight(args[1]);
                    Composer.load();
                    Composer.start_trade();
                    Composer.db_cleanse();
                    Mario.Quote.Get.run();
                    break;
                case "wipe":
                    preflight(args[1]);
                    Composer.load();
                    Composer.wipe_db();
                    Console.WriteLine("DONE.");
                    break;
                case "fix_avg":
                    Broker.fix_average_price("SOL-USD", "AAA");
                    break;
            }
            Console.ReadLine();
        }
    }
}