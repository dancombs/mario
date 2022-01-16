using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Timers;
using Mario.Workers;

namespace Mario.Setup
{
    public static class Composer
    {
        public static void load()
        {
            var list_symbols = new List<string>();
            Global.list_coin_objs = new Dictionary<string, Coin>();

            var results_lookback = Global.collection_trade_settings.
                Find(Global.filter_empty).
                Sort(Builders<BsonDocument>.Sort.Ascending("symbol")).
                ToList();

            if (results_lookback.Count == 0)
            {
                Console.WriteLine("=================================");
                Console.WriteLine("NO COIN(S) TO LOAD. CHECK MONGODB.");
                Console.WriteLine("=================================");
                return;
            }

            for (int counter_lookback = 0;
                 counter_lookback < results_lookback.Count;
                 counter_lookback++)
            {
                var symbol = (string)results_lookback[counter_lookback].GetValue("symbol");

                Coin coin = new Coin(
                    (string)results_lookback[counter_lookback].GetValue("_id").ToString(),
                    symbol,
                    (bool)results_lookback[counter_lookback].GetValue("locked"),
                    (decimal)results_lookback[counter_lookback].GetValue("cash_per_trade"),
                    (string)results_lookback[counter_lookback].GetValue("chain_id"),
                    (decimal)results_lookback[counter_lookback].GetValue("total_cash_to_play"),
                    (int)results_lookback[counter_lookback].GetValue("wait_buy_to_average_max"),
                    (int)results_lookback[counter_lookback].GetValue("wait_buy_to_average_min"),
                    (int)results_lookback[counter_lookback].GetValue("ta_history_period"),
                    (string)results_lookback[counter_lookback].GetValue("duration_candle")
                    );

                list_symbols.Add(symbol);

                Global.list_coin_objs.Add(symbol, coin);

                var collection =
                    Global.db_collection_prefix +
                    symbol +
                    Global.db_collection_quote;

                if (!CollectionExistsAsync(collection).Result)
                {
                    Global.mongodb_database.CreateCollectionAsync(collection);
                }

                coin.collection_quote =
                    Global.mongodb_database.GetCollection<BsonDocument>(collection);

                var collection_study =
                    Global.db_collection_prefix +
                    symbol +
                    Global.db_collection_study;

                if (!CollectionExistsAsync(collection_study).Result)
                {
                    Global.mongodb_database.CreateCollectionAsync(collection_study);
                }

                coin.collection_study =
                    Global.mongodb_database.GetCollection<BsonDocument>(collection_study);

            }

            Global.coin_list = string.Join(",", list_symbols);

        }

        public static void start_trade()
        {
            foreach (KeyValuePair<string, Coin> entry in Global.list_coin_objs)
            {
                entry.Value.start_worker_analysis();
                entry.Value.start_worker_buys();
                entry.Value.start_worker_sell();
            }
        }

        public static async Task<bool> CollectionExistsAsync(string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = await Global.mongodb_database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            return await collections.AnyAsync();
        }

        public static void db_cleanse()
        {
            DBClean_Worker.worker_db_clean = new BackgroundWorker()
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };

            DBClean_Worker.worker_db_clean.DoWork += DBClean_Worker.Init;
            DBClean_Worker.worker_db_clean.ProgressChanged += DBClean_Worker.worker_progress;
            DBClean_Worker.worker_db_clean.RunWorkerCompleted += DBClean_Worker.worker_completed;

            System.Timers.Timer timer_db_clean = new System.Timers.Timer();
            timer_db_clean.Elapsed += new ElapsedEventHandler(DBClean_Worker.timer_elapsed);
            timer_db_clean.Interval = 60000;
            timer_db_clean.Enabled = true;
            timer_db_clean.Start();
        }

        public static void wipe_db()
        {
            foreach (KeyValuePair<string, Coin> entry in Global.list_coin_objs)
            {
                var symbol = entry.Key;

                Global.mongodb_database.DropCollection(
                    Global.db_collection_prefix +
                    symbol +
                    Global.db_collection_quote
                    );

                Global.mongodb_database.DropCollection(
                    Global.db_collection_prefix +
                    symbol +
                    Global.db_collection_study
                    );
            }
        }
    }
}