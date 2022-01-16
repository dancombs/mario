using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using MongoDB.Driver;
using MongoDB.Bson;
using Coinbase.Pro;
using Coinbase.Pro.Models;
using Skender.Stock.Indicators;

namespace Mario.Study
{
    public static class Gonzo
    {
        public async static void run(
            IMongoCollection<BsonDocument> collection_study,
            string symbol,
            int duration_granuality,
            int duration_lookback_in_seconds
            )
        {
            var start = DateTime.Now.AddSeconds(duration_lookback_in_seconds);
            var end = DateTime.Now;

            List<Candle> candles = new List<Candle>();

            try
            {
                candles = await Global.coinbase_client.MarketData.GetHistoricRatesAsync(symbol, start, end, duration_granuality);
            }
            catch (Flurl.Http.FlurlHttpException e)
            {
                Console.WriteLine("=================================");
                Console.WriteLine("DISCONNECTED FROM COINBASE");
                Console.WriteLine("=================================");
                Console.WriteLine(e.Message);

                /********************/
                /* DOES NOT WORK 100% ON HTTP REFUSALS. SO MAYBE WAIT X MINUTES AND TRY AGAIN */
                /********************/

                foreach (KeyValuePair<string, Coin> entry in Global.list_coin_objs)
                {
                    Console.WriteLine("=================================");
                    Console.WriteLine("RESTARTING SERVICES");

                    entry.Value.stop_worker_analysis();
                    entry.Value.stop_worker_buys();
                    entry.Value.stop_worker_sell();

                    entry.Value.start_worker_analysis();
                    entry.Value.start_worker_buys();
                    entry.Value.start_worker_sell();
                }
                //Environment.Exit(0);
            }

            List<Skender.Stock.Indicators.Quote> _quotes = new List<Skender.Stock.Indicators.Quote>();

            for (int a = 0; a < candles.Count; a++)
            {
                Skender.Stock.Indicators.Quote quote = new Skender.Stock.Indicators.Quote();
                quote.Open = (decimal)candles[a].Open;
                quote.High = (decimal)candles[a].High;
                quote.Low = (decimal)candles[a].Low;
                quote.Close = (decimal)candles[a].Close;
                quote.Volume = (decimal)candles[a].Volume;
                quote.Date = DateTimeOffset.Parse(candles[a].Time.ToString()).UtcDateTime;
                _quotes.Add(quote);
            }

            var lookback = 300;

            IEnumerable<Skender.Stock.Indicators.Quote> history = _quotes.ToList<Skender.Stock.Indicators.Quote>();
            IEnumerable<SuperTrendResult> results_super = Skender.Stock.Indicators.Indicator.GetSuperTrend(history).TakeLast(lookback);

            var dateTime = DateTime.Now.ToUniversalTime();

            /*************************************
             * 
             * PARSE THE SUPERTREND RESULTS.
             * AND DUMP RESULTS TO MONGODB IF IT'S A BUY OR SELL SIGNAL
             * 
             *************************************/

            BsonDocument doc = new()
            {
                { "symbol", new BsonString(symbol) },
                { "read", new BsonBoolean(false) },
                { "0_quoteTime", new BsonDateTime(dateTime) },
                { "supertrend_signal", new BsonString("BUY_OR_SELL") }
            };

            collection_study.InsertOneAsync(doc);
        }
    }
}