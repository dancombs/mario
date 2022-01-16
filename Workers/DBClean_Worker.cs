using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Net.Http;
using System.Web;
using System.ComponentModel;
using System.Timers;

namespace Mario.Workers
{
    static class DBClean_Worker
    {
        public static BackgroundWorker worker_db_clean;

        public static void timer_elapsed(object sender, ElapsedEventArgs e)
        {
            if (!worker_db_clean.IsBusy)
            {
                worker_db_clean.RunWorkerAsync();
            }
            else
            {
                worker_db_clean.CancelAsync();
            }
        }
        public static void worker_progress(object sender, ProgressChangedEventArgs e)
        {
            //display the progress using e.ProgressPercentage and/or e.UserState
        }
        public static void worker_completed(object sender, RunWorkerCompletedEventArgs e)
        {
            /*
            if (e.Cancelled)
            {

            }
            else
            {

            }
            */
        }

        public static async void Init(object sender, DoWorkEventArgs e)
        {
            if (worker_db_clean.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            foreach (KeyValuePair<string, Coin> entry in Global.list_coin_objs)
            {
                Coin coin = Global.list_coin_objs[entry.Key];

                var collection = coin.collection_quote.Find(Global.filter_empty).
                    Sort(Builders<BsonDocument>.
                    Sort.Descending("quoteTimeInLong")).ToList();

                BsonValue date_removal_index = null;

                if (collection.Count() >
                    coin.ta_history_period)
                {
                    date_removal_index = collection[coin.ta_history_period].GetValue("quoteTimeInLong");

                    var filter_date = Builders<BsonDocument>.Filter.Lt("quoteTimeInLong", date_removal_index);

                    collection = coin.collection_quote.Find(filter_date).
                        Sort(Builders<BsonDocument>.
                        Sort.Ascending("quoteTimeInLong")).ToList();

                    coin.collection_quote.DeleteMany(filter_date);
                }

                collection = coin.collection_study.Find(Global.filter_empty).
                    Sort(Builders<BsonDocument>.
                    Sort.Descending("0_quoteTime")).ToList();

                if (collection.Count() >
                    coin.ta_history_period)
                {
                    date_removal_index = collection[coin.ta_history_period].GetValue("0_quoteTime");

                    var filter_date = Builders<BsonDocument>.Filter.Lt("0_quoteTime", date_removal_index);

                    collection = coin.collection_study.Find(filter_date).
                        Sort(Builders<BsonDocument>.
                        Sort.Ascending("0_quoteTime")).ToList();

                    coin.collection_study.DeleteMany(filter_date);

                }
            }
        }
    }
}