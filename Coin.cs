using System;
using System.ComponentModel;
using System.Timers;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net.Http;
using System.Net.Http.Headers;

using Coinbase.Pro;
using Coinbase.Pro.Models;
using Mario.Study;
using Mario.Trade;

namespace Mario
{
    public class Coin
    {
        public System.Timers.Timer timer_analysis;
        public BackgroundWorker back_worker_analysis;

        public System.Timers.Timer timer_buy;
        public BackgroundWorker back_worker_buy;

        public System.Timers.Timer timer_sell;
        public BackgroundWorker back_worker_sell;

        public IMongoCollection<BsonDocument> collection_quote;
        public IMongoCollection<BsonDocument> collection_study;

        // GETTERS/SETTERS
        public decimal _shares_per_trade { set; get; }
        public decimal _total_shares_to_play { set; get; }
        public int _wait_buy_to_average_max { set; get; }
        public int _wait_buy_to_average_min { set; get; }
        public int _ta_history_period { set; get; }

        // ATTRIBUTES
        public string _id;
        public bool locked;
        public decimal cash_per_trade;
        public string symbol;
        public string chain_id;
        public decimal total_cash_to_play;
        public int wait_buy_to_average_max;
        public int wait_buy_to_average_min;
        public int ta_history_period;
        // CANDLE DURATION
        public int duration_granuality;
        public int duration_lookback_in_seconds;

        public Coin(string _id,
              string symbol,
              bool locked,
              decimal cash_per_trade,
              string chain_id,
              decimal total_cash_to_play,
              int wait_buy_to_average_max,
              int wait_buy_to_average_min,
              int ta_history_period,
              string duration_candle
            )
        {
            this._id = _id;
            this.symbol = symbol;
            this.locked = locked;
            this.cash_per_trade = cash_per_trade;
            this.chain_id = chain_id;
            this.total_cash_to_play = total_cash_to_play;
            this.wait_buy_to_average_max = wait_buy_to_average_max;
            this.wait_buy_to_average_min = wait_buy_to_average_min;
            this.ta_history_period = ta_history_period;

            /*
             * The granularity field must be one of the following values: {60, 300, 900, 3600, 21600, 86400}. 
             * Otherwise, your request will be rejected. These values correspond to timeslices representing 
             * one minute, five minutes, fifteen minutes, one hour, six hours, and one day, respectively.
            */
            switch (duration_candle)
            {
                case "1min":
                    // -5 hours. 10800 sec = 299 records
                    this.duration_granuality = 60;
                    this.duration_lookback_in_seconds = -10800;
                    break;
                case "5min":
                    // -25 hours. 90000 sec = 299 records
                    this.duration_granuality = 300;
                    this.duration_lookback_in_seconds = -86400;
                    break;
                case "15min":
                    // -75 hours. 270000 sec = 299 records
                    this.duration_granuality = 900;
                    this.duration_lookback_in_seconds = -270000;
                    break;
                case "1hr":
                    // -300 hours. 1080000 sec = 299 records
                    this.duration_granuality = 3600;
                    this.duration_lookback_in_seconds = -756000;
                    break;
                case "6hrs":
                    // -1800 hours. 6480000 sec = 299 records
                    this.duration_granuality = 21600;
                    this.duration_lookback_in_seconds = -6480000;
                    break;
                case "1day":
                    // -7200 hours. 25920000 sec = 299 records
                    this.duration_granuality = 86400;
                    this.duration_lookback_in_seconds = -25920000;
                    break;
            }
        }

        /************************************************
         TA 
        ************************************************/

        public void timer_elapsed_analysis(object sender, ElapsedEventArgs e)
        {

            if (!back_worker_analysis.IsBusy)
            {
                back_worker_analysis.RunWorkerAsync();
            }
            else
            {
                back_worker_analysis.CancelAsync();
                /*
                Console.WriteLine("===============================");
                Console.WriteLine("STUDY TA - BUSY");
                Console.WriteLine("===============================");
                */
            }
        }

        public void worker_progress_analysis(object sender, ProgressChangedEventArgs e)
        {
            //display the progress using e.ProgressPercentage and/or e.UserState
        }

        public void worker_completed_analysis(object sender, RunWorkerCompletedEventArgs e)
        {
            /*
            Console.WriteLine("===============================");
            Console.WriteLine("STUDY TA - DONE");
            Console.WriteLine("===============================");
            */
            if (e.Cancelled)
            {

            }
            else
            {

            }
        }

        public async void worker_start_analysis(object sender, DoWorkEventArgs e)
        {
            if (back_worker_analysis.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            
            Gonzo.run(
                   collection_study,
                   symbol,
                   this.duration_granuality,
                   this.duration_lookback_in_seconds
                   );
            
        }

        public void start_worker_analysis()
        {
            back_worker_analysis = new BackgroundWorker()
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };

            back_worker_analysis.DoWork += worker_start_analysis;
            back_worker_analysis.ProgressChanged += worker_progress_analysis;
            back_worker_analysis.RunWorkerCompleted += worker_completed_analysis;

            timer_analysis = new System.Timers.Timer();
            timer_analysis.Elapsed += new ElapsedEventHandler(timer_elapsed_analysis);
            timer_analysis.Interval = 1000;
            timer_analysis.Enabled = true;
            timer_analysis.Start();
        }

        public void stop_worker_analysis()
        {
            back_worker_analysis.CancelAsync();
            timer_analysis.Stop();
        }

        /************************************************
         BUY
        ************************************************/

        public void timer_elapsed_buy(object sender, ElapsedEventArgs e)
        {
            if (!back_worker_buy.IsBusy)
            {
                back_worker_buy.RunWorkerAsync();
            }
            else
            {
                back_worker_buy.CancelAsync();
                /*
                Console.WriteLine("===============================");
                Console.WriteLine("STUDY TA - BUSY");
                Console.WriteLine("===============================");
                */
            }
        }

        public void worker_progress_buy(object sender, ProgressChangedEventArgs e)
        {
            //display the progress using e.ProgressPercentage and/or e.UserState
        }

        public void worker_completed_buy(object sender, RunWorkerCompletedEventArgs e)
        {
            /*
            Console.WriteLine("===============================");
            Console.WriteLine("STUDY TA - DONE");
            Console.WriteLine("===============================");
            */
            if (e.Cancelled)
            {

            }
            else
            {

            }
        }

        public async void worker_start_buy(object sender, DoWorkEventArgs e)
        {
            Broker.look_for_buys(
                collection_study,
                timer_buy,
                back_worker_buy,
                symbol,
                Broker.get_latest_ask_price(collection_quote, symbol),
                total_cash_to_play,
                cash_per_trade,
                wait_buy_to_average_max,
                wait_buy_to_average_min,
                chain_id,
                Broker.get_latest_price(collection_quote, symbol));
        }

        public void start_worker_buys()
        {
            back_worker_buy = new BackgroundWorker()
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };

            back_worker_buy.DoWork += worker_start_buy;
            back_worker_buy.ProgressChanged += worker_progress_buy;
            back_worker_buy.RunWorkerCompleted += worker_completed_buy;

            timer_buy = new System.Timers.Timer();
            timer_buy.Elapsed += new ElapsedEventHandler(timer_elapsed_buy);
            timer_buy.Interval = 2500;
            timer_buy.Enabled = true;
            timer_buy.Start();
        }

        public void stop_worker_buys()
        {
            back_worker_buy.CancelAsync();
            timer_buy.Stop();
        }

        /************************************************
         SELL
        ************************************************/

        public void timer_elapsed_sell(object sender, ElapsedEventArgs e)
        {
            if (!back_worker_sell.IsBusy)
            {
                back_worker_sell.RunWorkerAsync();
            }
            else
            {
                back_worker_sell.CancelAsync();
                /*
                Console.WriteLine("===============================");
                Console.WriteLine("STUDY TA - BUSY");
                Console.WriteLine("===============================");
                */
            }
        }

        public void worker_progress_sell(object sender, ProgressChangedEventArgs e)
        {
            //display the progress using e.ProgressPercentage and/or e.UserState
        }

        public void worker_completed_sell(object sender, RunWorkerCompletedEventArgs e)
        {
            /*
            Console.WriteLine("===============================");
            Console.WriteLine("STUDY TA - DONE");
            Console.WriteLine("===============================");
            */
            if (e.Cancelled)
            {

            }
            else
            {

            }
        }

        public async void worker_start_sell(object sender, DoWorkEventArgs e)
        {

            if (back_worker_sell.CancellationPending | Broker.is_trade_locked(symbol))
            {
                e.Cancel = true;
                return;
            }

            Broker.look_for_sells(
                collection_study,
                symbol,
                Broker.get_latest_bid_price(collection_quote, symbol),
                total_cash_to_play,
                cash_per_trade,
                timer_sell,
                back_worker_sell,
                wait_buy_to_average_max,
                wait_buy_to_average_min,
                chain_id,
                Broker.get_latest_price(collection_quote, symbol)
                );

        }

        public void start_worker_sell()
        {
            back_worker_sell = new BackgroundWorker()
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };

            back_worker_sell.DoWork += worker_start_sell;
            back_worker_sell.ProgressChanged += worker_progress_sell;
            back_worker_sell.RunWorkerCompleted += worker_completed_sell;

            timer_sell = new System.Timers.Timer();
            timer_sell.Elapsed += new ElapsedEventHandler(timer_elapsed_sell);
            timer_sell.Interval = 500;
            timer_sell.Enabled = true;
            timer_sell.Start();
        }

        public void stop_worker_sell()
        {
            back_worker_sell.CancelAsync();
            timer_sell.Stop();
        }

    }
}