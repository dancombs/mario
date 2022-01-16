using System;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Net.Http;
using System.Net.Http.Headers;
using Coinbase.Pro;
using Coinbase.Pro.Models;

namespace Mario.Trade
{
    public static class Order
    {
        public static async void Trade(
            string symbol,
            string buy_or_sell,
            decimal cash_per_trade,
            string chain_id
            )
        {
            Broker.set_trade_locked(symbol, true);

            Coinbase.Pro.Models.Order order;

            if (buy_or_sell.Equals("Buy"))
            {
                order = await Global.coinbase_client.Orders.PlaceMarketOrderAsync(
                   OrderSide.Buy,
                   symbol,
                   cash_per_trade,
                   AmountType.UseFunds);
            }
            else
            {
                order = await Global.coinbase_client.Orders.PlaceMarketOrderAsync(
                   OrderSide.Sell,
                   symbol,
                   Broker.get_shares_owned(symbol));
            }

            System.Threading.Thread.Sleep(new Random().Next(15, 30) * 1000);

            var r = await Global.coinbase_client.Fills.GetFillsByOrderIdAsync(order.Id);
            List<Fill> filled = r.Data.ToList();

            if (filled.Count >= 2)
            {
                if (buy_or_sell.Equals("Buy"))
                {
                    for (int a = 0; a < filled.Count; a++)
                    {
                        if (filled[a].OrderId.Equals(order.Id))
                        {

                            if (a == filled.Count - 1)
                            {
                                Broker.log_trade(
                                    symbol,
                                    buy_or_sell,
                                    filled[a].Price,
                                    (decimal)Convert.ToDecimal(filled[a].Size),
                                    "M-" + filled[a].OrderId,
                                    filled[a].Fee,
                                    cash_per_trade,
                                    filled[a].TradeId,
                                    0.00M
                                    );
                            }
                            else
                            {
                                Broker.log_trade(
                                    symbol,
                                    buy_or_sell,
                                    filled[a].Price,
                                    (decimal)Convert.ToDecimal(filled[a].Size),
                                    "M-" + filled[a].OrderId,
                                    filled[a].Fee,
                                    0M,
                                    filled[a].TradeId,
                                    0.00M
                                    );
                            }
                        }
                    }
                }

                else
                {
                    decimal i_sold_value_total = 0;

                    decimal shares_owned = Broker.get_shares_owned(symbol);
                    decimal shares_owned_value = shares_owned * Broker.get_average_price(symbol);

                    for (int a = 0; a < filled.Count; a++)
                    {
                        if (filled[a].OrderId.Equals(order.Id))
                        {
                            decimal i_price = filled[a].Price;
                            decimal i_size = (decimal)Convert.ToDecimal(filled[a].Size);

                            decimal i_sold_value = i_price * i_size;
                            i_sold_value_total += i_sold_value;

                            if (a == filled.Count - 1)
                            {
                                var profit_total = i_sold_value_total - shares_owned_value;

                                Broker.log_trade(
                                    symbol,
                                    buy_or_sell,
                                    filled[a].Price,
                                    (decimal)Convert.ToDecimal(filled[a].Size),
                                    "M-" + filled[a].OrderId,
                                    filled[a].Fee,
                                    i_sold_value_total,
                                    filled[a].TradeId,
                                    profit_total
                                    );
                            }
                            else
                            {
                                Broker.log_trade(
                                    symbol,
                                    buy_or_sell,
                                    filled[a].Price,
                                    (decimal)Convert.ToDecimal(filled[a].Size),
                                    "M-" + filled[a].OrderId,
                                    filled[a].Fee,
                                    i_sold_value_total,
                                    filled[a].TradeId,
                                    0.00M
                                    );
                            }
                        }
                    }
                }
                //Broker.fix_average_price(symbol, chain_id);
            }
            else
            {
                if (buy_or_sell.Equals("Buy"))
                {

                    Broker.log_trade(
                        symbol,
                        buy_or_sell,
                        filled[0].Price,
                        (decimal)Convert.ToDecimal(filled[0].Size),
                        filled[0].OrderId,
                        filled[0].Fee,
                        cash_per_trade,
                        filled[0].TradeId,
                        0.00M
                        );
                }
                else
                {
                    decimal shares_owned = Broker.get_shares_owned(symbol);
                    decimal shares_owned_value = shares_owned * Broker.get_average_price(symbol);

                    decimal sold_price = ((decimal)Convert.ToDecimal(filled[0].Size) * filled[0].Price) - shares_owned_value;

                    Broker.log_trade(
                        symbol,
                        buy_or_sell,
                        filled[0].Price,
                        (decimal)Convert.ToDecimal(filled[0].Size),
                        filled[0].OrderId,
                        filled[0].Fee,
                        cash_per_trade,
                        filled[0].TradeId,
                        sold_price
                        );
                }
            }

            if (buy_or_sell.Equals("Sell"))
            {
                Broker.clear_chain_id(symbol);
            }

            Broker.set_trade_locked(symbol, false);

        }
    }
}