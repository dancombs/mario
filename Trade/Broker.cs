using MongoDB.Driver;
using MongoDB.Bson;
using System.Timers;
using System.ComponentModel;
using Timer = System.Timers.Timer;
using Skender.Stock.Indicators;

namespace Mario.Trade
{
    public static class Broker
    {
        public static void commit_trade(string symbol, string buy_or_sell, decimal cash_per_trade, string chain_id)
        {
            Order.Trade(symbol, buy_or_sell, cash_per_trade, chain_id);
        }

        public static void look_for_buys(
            IMongoCollection<BsonDocument> collection_study,
            System.Timers.Timer timer_buy,
            BackgroundWorker back_worker_buy,
            string symbol,
            decimal last_ask_price,
            decimal total_cash_to_play,
            decimal cash_per_trade,
            int wait_buy_to_average_max,
            int wait_buy_to_average_min,
            string chain_id,
            decimal last_price)
        {
            var can_buy_ = can_buy(symbol, total_cash_to_play, cash_per_trade);

            if (can_buy_ == false || last_ask_price == 0)
            {
                return;
            }

            var average_price = get_average_price(symbol);
            bool should_buy = false;

            DateTime datetime_current = DateTime.Now.ToUniversalTime();
            DateTime datetime_past = datetime_current.AddMinutes(-2).ToLocalTime();
            DateTime last_purchase = DateTime.Now;

            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter_symbol = filterBuilder.Eq("symbol", symbol);
            var filter_date = filterBuilder.Gte("0_quoteTime", datetime_past);

            var results_ = collection_study.
                Find(
                filter_symbol &
                filter_date
                ).
                Sort(Builders<BsonDocument>.Sort.Descending("0_quoteTime")).
                ToList();

            for (var counter_a = 0; counter_a < results_.Count; counter_a++)
            {
                // IF LASTEST RECORD HAS BEEN READ - DO NOTHING.
                if(counter_a == 0 && results_[counter_a].GetValue("read") == true)
                {
                    break;
                }

                if (results_[counter_a].GetValue("supertrend_signal").Equals("BUY"))
                {
                    should_buy = true;
                }

                var _id = results_[counter_a].GetValue("_id").ToString();
                var filter_by_id = filterBuilder.Eq("_id", ObjectId.Parse(_id));
                var update_analyze = Builders<BsonDocument>.Update.Set("read", true);
                collection_study.UpdateOne(filter_by_id, update_analyze);
            }
            
            if(should_buy)
            {
                if (average_price == 0 &&
                can_buy_)
                {
                    Util.trade_alarm("Buy");
                    Console.WriteLine("====================================");
                    Console.WriteLine("BOUGHT..." + last_purchase.ToUniversalTime() + " at " + DateTime.Now.ToLocalTime());

                    Thread myNewThread = new Thread(() => commit_trade(symbol, "Buy", cash_per_trade, chain_id));
                    myNewThread.Start();

                }
                else if (average_price > 0 &&
                    can_buy_ &&
                    (percent_difference_price(get_average_price(symbol), last_ask_price)
                    <= 0))
                {
                    Util.trade_alarm("Buy");
                    Console.WriteLine("====================================");
                    Console.WriteLine("BOUGHT AVG UPPED..." + DateTime.Now.ToLocalTime());

                    Thread myNewThread = new Thread(() => commit_trade(symbol, "Buy", cash_per_trade, chain_id));
                    myNewThread.Start();
                }
            }

            // DELAY THE AVERAGE DOWN
            /*
            back_worker_buy.CancelAsync();
            timer_buy.Stop();
            System.Threading.Thread.Sleep(new Random().Next(wait_buy_to_average_min, wait_buy_to_average_max) * 1000);
            timer_buy.Start();
            */
        }

        public static void look_for_sells(
            IMongoCollection<BsonDocument> collection_study,
            string symbol,
            decimal last_bid_price,
            decimal total_cash_to_play,
            decimal cash_per_trade,
            Timer timer_sell,
            BackgroundWorker back_worker_sell,
            int wait_buy_to_average_max,
            int wait_buy_to_average_min,
            string chain_id,
            decimal last_price)
        {

            var can_sell_ = can_sell(symbol, total_cash_to_play, cash_per_trade);

            if (can_sell_ == false || last_bid_price == 0)
            {
                return;
            }

            var average_price = get_average_price(symbol);

            var cost_of_all_trades = get_cost_of_trades(symbol);
            var sell_fee = (.5M / 100M) * (cost_of_all_trades);
            var bought_fees = get_bought_fees(symbol);
            var tally = cost_of_all_trades + sell_fee + bought_fees;

            DateTime datetime_current = DateTime.Now.ToUniversalTime();
            DateTime datetime_past = datetime_current.AddSeconds(-10).ToLocalTime();
            DateTime last_purchase = DateTime.Now;

            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter_symbol = filterBuilder.Eq("symbol", symbol);
            var filter_sell_signal = filterBuilder.Eq("supertrend_signal", "SELL");
            var filter_date = filterBuilder.Gte("0_quoteTime", datetime_past);

            var results_ = collection_study.
                Find(
                filter_symbol &
                filter_date &
                filter_sell_signal
                ).
                Sort(Builders<BsonDocument>.Sort.Descending("0_quoteTime")).
                ToList();

            can_sell_ = can_sell(symbol, total_cash_to_play, cash_per_trade);

            if (results_.Count() > 0 &&
                percent_difference_price(average_price, last_bid_price) > 0 &&
                (get_shares_owned(symbol) * last_bid_price) > tally)
            {
                Util.trade_alarm("Sell");
                Console.WriteLine("====================================");
                Console.WriteLine("SOLD..." + DateTime.Now.ToLocalTime());

                Thread myNewThread = new Thread(() => commit_trade(symbol, "Sell", get_spent_amount(symbol), chain_id));
                myNewThread.Start();
            }
        }

        public static void log_trade(
            string symbol,
            string buy_or_sell,
            decimal price,
            decimal quantity,
            string order_id,
            decimal fee,
            decimal cost,
            decimal trade_id,
            decimal profit)
        {
            string chainID = get_last_chain_id(symbol);

            /* IF CHAIN ID IS EMPTY - START A NEW TRADE SERIES. 
               SHOULD ALWAYS DEFAULT TO BUY */
            if (chainID.Equals(""))
            {
                BsonDocument doc = new BsonDocument{
                            {"timestamp", new BsonDateTime(DateTime.Now.ToLocalTime())},
                            {"trade_id", new BsonDecimal128(trade_id)},
                            {"symbol", new BsonString(symbol)},
                            {"order_id", new BsonString(order_id)},
                            //{"chain_id", new BsonString(chainID)},
                            {"action", new BsonString(buy_or_sell)},
                            {"price", new Decimal128(price)},
                            {"quantity", new Decimal128((decimal) Convert.ToDecimal(quantity))},
                            {"average_price", new Decimal128((price * quantity)/quantity)},
                            {"fee", new Decimal128(fee)},
                            {"cost", new Decimal128(cost)},
                            {"cost_w_fee", new Decimal128(fee+cost)}
                };

                Global.collection_trade_log.InsertOne(doc);
                /******************************
                MONGO DB ID IS ONLY AVAILABLE AFTER THE "INSERTONE" CALL TO GET BSON ID.
                SO IT'S A POST UPDATE OPERATION AFTER THE INSERT.
                *******************************/
                write_last_chain_id(symbol, doc.GetValue("_id").ToString());
            }
            else
            {
                var shares_owned_value = get_shares_owned(symbol) * get_average_price(symbol);
                var shares_sold_value = (price * quantity);
                var sell_avg = shares_sold_value / quantity;

                switch (buy_or_sell)
                {
                    case "Buy":
                        Global.collection_trade_log.InsertOne(new BsonDocument{
                            {"timestamp", new BsonDateTime(DateTime.Now.ToLocalTime())},
                            {"trade_id", new BsonDecimal128(trade_id)},
                            {"symbol", new BsonString(symbol)},
                            {"chain_id", new BsonString(chainID)},
                            {"order_id", new BsonString(order_id)},
                            {"action", new BsonString(buy_or_sell)},
                            {"price", new Decimal128(price)},
                            {"quantity", new Decimal128((decimal) Convert.ToDecimal(quantity))},
                            {"average_price", new BsonDecimal128(set_average_price(symbol, chainID, price, quantity))},
                            {"fee", new Decimal128(fee)},
                            {"cost", new Decimal128(cost)},
                            {"cost_w_fee", new Decimal128(fee+cost)},
                            {"average_price_old", new BsonDecimal128(set_average_price(symbol, chainID, price, quantity))},
                            });
                        break;

                    case "Sell":
                        Global.collection_trade_log.InsertOne(new BsonDocument{
                                {"timestamp", new BsonDateTime(DateTime.Now.ToLocalTime())},
                                {"trade_id", new BsonDecimal128(trade_id)},
                                {"symbol", new BsonString(symbol)},
                                {"chain_id", new BsonString(chainID)},
                                {"order_id", new BsonString(order_id)},
                                {"action", new BsonString(buy_or_sell)},
                                {"price", new Decimal128(price)},
                                {"quantity", new Decimal128((decimal) Convert.ToDecimal(quantity))},
                                {"average_price", new BsonDecimal128(sell_avg)},
                                {"fee", new Decimal128(fee)},
                                //{"profit", new Decimal128(shares_sold_value - shares_owned_value) },
                                {"profit", new Decimal128(profit) },
                            });
                        break;
                }
            }
        }

        public static bool can_buy(string symbol, decimal total_cash_to_play, decimal cash_per_trade)
        {
            bool return_val = false;

            if (is_trade_locked(symbol))
            {
                /*
                Console.WriteLine("=================================");
                Console.WriteLine("NO TRADE: LOCKED");
                Console.WriteLine("=================================");
                */
                return false;
            }

            decimal spent_amount = get_spent_amount(symbol);
            if (spent_amount >= total_cash_to_play)
            {
                /*
                Console.WriteLine("=================================");
                Console.WriteLine("NO TRADE: MAXED OUT SHARES");
                Console.WriteLine("=================================");
                */
                return false;
            }

            if (total_cash_to_play - spent_amount >= cash_per_trade)
            {
                return true;
            }

            return return_val;
        }

        public static bool can_sell(string symbol, decimal total_cash_to_play, decimal cash_per_trade)
        {
            bool return_val = false;

            if (is_trade_locked(symbol))
            {
                /*
                Console.WriteLine("=================================");
                Console.WriteLine("NO TRADE: LOCKED");
                Console.WriteLine("=================================");
                */
                return false;
            }

            decimal spent_amount = get_spent_amount(symbol);
            if (spent_amount >= cash_per_trade)
            {
                return_val = true;
            }

            return return_val;
        }

        public static decimal get_latest_price(IMongoCollection<BsonDocument> collection_quote, string symbol)
        {
            decimal return_val;

            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);

            var record = collection_quote.Find(
                                      filter_symbol).
                                      Sort(Builders<BsonDocument>.
                                      Sort.Descending("quoteTimeInLong")).
                                      Limit(1).ToList();

            if (record.Count == 0)
            {
                return_val = 0;
            }
            else
            {
                return_val = (decimal)record[0].GetValue("lastPrice");
            }

            return return_val;
        }

        public static decimal get_latest_ask_price(IMongoCollection<BsonDocument> collection_quote, string symbol)
        {
            decimal return_val;

            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);

            var record = collection_quote.Find(
                                      filter_symbol).
                                      Sort(Builders<BsonDocument>.
                                      Sort.Descending("quoteTimeInLong")).
                                      Limit(1).ToList();

            if (record.Count == 0)
            {
                return_val = 0;
            }
            else
            {
                return_val = (decimal)record[0].GetValue("askPrice");
            }

            return return_val;
        }

        public static decimal get_latest_bid_price(IMongoCollection<BsonDocument> collection_quote, string symbol)
        {
            decimal return_val;

            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);

            var record = collection_quote.Find(
                                      filter_symbol).
                                      Sort(Builders<BsonDocument>.
                                      Sort.Descending("quoteTimeInLong")).
                                      Limit(1).ToList();

            if (record.Count == 0)
            {
                return_val = 0;
            }
            else
            {
                return_val = (decimal)record[0].GetValue("bidPrice");
            }

            return return_val;
        }

        public static decimal get_shares_owned(string symbol)
        {
            decimal return_val = 0;

            string chainID = get_last_chain_id(symbol);

            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);
            var filter_chainID = Builders<BsonDocument>.Filter.Eq("chain_id", chainID);

            FilterDefinition<BsonDocument> filter_trade = null;

            filter_trade = Builders<BsonDocument>.Filter.Eq("action", "Buy");

            var results_lookback = Global.collection_trade_log.Find(
                filter_symbol &
                filter_chainID &
                filter_trade).ToList();

            for (int counter_lookback = 0; counter_lookback < results_lookback.Count; counter_lookback++)
            {
                return_val += (decimal)results_lookback[counter_lookback].GetValue("quantity");
            }

            return return_val;
        }

        public static decimal get_spent_amount(string symbol)
        {
            decimal return_val = 0;

            string chainID = get_last_chain_id(symbol);

            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);
            var filter_chainID = Builders<BsonDocument>.Filter.Eq("chain_id", chainID);

            FilterDefinition<BsonDocument> filter_trade = null;

            filter_trade = Builders<BsonDocument>.Filter.Eq("action", "Buy");

            var results_lookback = Global.collection_trade_log.Find(
                filter_symbol &
                filter_chainID &
                filter_trade).ToList();

            for (int counter_lookback = 0; counter_lookback < results_lookback.Count; counter_lookback++)
            {
                return_val += (decimal)results_lookback[counter_lookback].GetValue("cost");
            }

            return return_val;
        }

        public static void fix_average_price(string symbol, string chainID)
        {

            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);
            var filter_chainID = Builders<BsonDocument>.Filter.Eq("chain_id", chainID);
            FilterDefinition<BsonDocument> filter_trade = null;

            filter_trade = Builders<BsonDocument>.Filter.Eq("action", "Buy");

            var results_lookback = Global.collection_trade_log.Find(
                filter_symbol &
                filter_chainID &
                filter_trade).
                Sort(Builders<BsonDocument>.Sort.Ascending("timestamp")).ToList();

            decimal average_price = 0;
            decimal total_price = 0;
            decimal total_shares = 0;

            Console.WriteLine("=========================");
            for (int counter = 0; counter < results_lookback.Count; counter++)
            {
                var filterBuilder = Builders<BsonDocument>.Filter;
                string filter_by_id_string = (string)results_lookback[counter].GetValue("_id").ToString();
                var filter_by_id = filterBuilder.Eq("_id", ObjectId.Parse(filter_by_id_string));

                var quantity = (decimal)results_lookback[counter].GetValue("quantity");
                var price = (decimal)results_lookback[counter].GetValue("price");

                total_price += quantity * price;
                total_shares += quantity;

                average_price = total_price / total_shares;

                BsonDecimal128 value = new BsonDecimal128(average_price);
                var update_average = Builders<BsonDocument>.Update.Set("average_price", value);
                Global.collection_trade_log.UpdateOne(filter_by_id, update_average);

            }
        }

        public static decimal set_average_price(string symbol, string chainID, decimal new_price, decimal new_quantity)
        {
            decimal average_price = 0;

            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);
            var filter_chainID = Builders<BsonDocument>.Filter.Eq("chain_id", chainID);
            FilterDefinition<BsonDocument> filter_trade = null;

            filter_trade = Builders<BsonDocument>.Filter.Eq("action", "Buy");

            var results_lookback = Global.collection_trade_log.Find(
                filter_symbol &
                filter_chainID &
                filter_trade).Sort(Builders<BsonDocument>.Sort.Ascending("timestamp")).ToList();

            if (results_lookback.Count == 0)
            {
                return 0;
            }
            else
            {
                decimal total_price = 0;
                decimal total_shares = 0;

                for (int counter_lookback = 0; counter_lookback < results_lookback.Count; counter_lookback++)
                {
                    var quantity = (decimal)results_lookback[counter_lookback].GetValue("quantity");
                    var price = (decimal)results_lookback[counter_lookback].GetValue("price");

                    total_price += quantity * price;
                    total_shares += quantity;
                    average_price = total_price / total_shares;
                }
                total_price += new_quantity * new_price;
                total_shares += new_quantity;
                average_price = total_price / total_shares;

                Console.WriteLine(average_price);
            }
            return average_price;
        }

        public static decimal get_average_price(string symbol)
        {
            string chainID = get_last_chain_id(symbol);

            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);
            var filter_chainID = Builders<BsonDocument>.Filter.Eq("chain_id", chainID);
            FilterDefinition<BsonDocument> filter_trade = null;

            filter_trade = Builders<BsonDocument>.Filter.Eq("action", "Buy");

            var result_last_record = Global.collection_trade_log.Find(
                filter_symbol &
                filter_chainID &
                filter_trade).
                Limit(1).
                Sort(Builders<BsonDocument>.Sort.Descending("timestamp")).
                ToList();

            if (result_last_record.Count() == 0)
            {
                return 0;
            }
            else
            {
                return (decimal)result_last_record[0].GetValue("average_price");
            }
        }

        public static string get_last_chain_id(string symbol)
        {
            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);
            var result_lookback = Global.collection_trade_settings.Find(filter_symbol).Single();
            return (string)result_lookback.GetValue("chain_id");
        }

        public static void write_last_chain_id(string symbol, string chainID)
        {
            var filterBuilder = Builders<BsonDocument>.Filter;
            var update_chainID = Builders<BsonDocument>.Update.Set("chain_id", chainID);

            var filter_by_symbol = filterBuilder.Eq("symbol", symbol);
            Global.collection_trade_settings.UpdateOne(filter_by_symbol, update_chainID);
            // LOG DOESN'T HAVE A CHAIN ID WRITTEN...SO WRITE ONE
            var filter_by_id = filterBuilder.Eq("_id", ObjectId.Parse(chainID));
            Global.collection_trade_log.UpdateOne(filter_by_id, update_chainID);
        }

        public static void clear_chain_id(string symbol)
        {
            var filterBuilder = Builders<BsonDocument>.Filter;
            var update_chainID = Builders<BsonDocument>.Update.Set("chain_id", "");
            var filter_by_symbol = filterBuilder.Eq("symbol", symbol);
            Global.collection_trade_settings.UpdateOne(filter_by_symbol, update_chainID);
        }

        public static void set_trade_locked(string symbol, bool lock_value)
        {
            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);
            var update = Builders<BsonDocument>.Update.Set("locked", lock_value);
            Global.collection_trade_settings.UpdateOne(filter_symbol, update);
        }

        public static bool is_trade_locked(string symbol)
        {
            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);
            var result_lookback = Global.collection_trade_settings.Find(filter_symbol).Single();
            var is_locked = (bool)result_lookback.GetValue("locked");
            return is_locked;
        }

        public static decimal percent_difference_price(decimal oldPrice, decimal newPrice)
        {
            return ((newPrice - oldPrice) / oldPrice) * 100;
        }

        public static decimal get_bought_fees(string symbol)
        {
            decimal return_val = 0;

            string chainID = get_last_chain_id(symbol);

            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);
            var filter_chainID = Builders<BsonDocument>.Filter.Eq("chain_id", chainID);

            FilterDefinition<BsonDocument> filter_trade = null;

            filter_trade = Builders<BsonDocument>.Filter.Eq("action", "Buy");

            var results_lookback = Global.collection_trade_log.Find(
                filter_symbol &
                filter_chainID &
                filter_trade).ToList();

            for (int counter_lookback = 0; counter_lookback < results_lookback.Count; counter_lookback++)
            {
                return_val += (decimal)results_lookback[counter_lookback].GetValue("fee");
            }

            return return_val;
        }

        public static decimal get_cost_of_trades(string symbol)
        {
            decimal return_val = 0;

            string chainID = get_last_chain_id(symbol);

            var filter_symbol = Builders<BsonDocument>.Filter.Eq("symbol", symbol);
            var filter_chainID = Builders<BsonDocument>.Filter.Eq("chain_id", chainID);

            FilterDefinition<BsonDocument> filter_trade = null;

            filter_trade = Builders<BsonDocument>.Filter.Eq("action", "Buy");

            var results_lookback = Global.collection_trade_log.Find(
                filter_symbol &
                filter_chainID &
                filter_trade).ToList();

            for (int counter_lookback = 0; counter_lookback < results_lookback.Count; counter_lookback++)
            {
                return_val += (decimal)results_lookback[counter_lookback].GetValue("cost");
            }

            return return_val;
        }
    }
}