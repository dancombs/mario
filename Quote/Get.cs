using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Net.Http;
using System;
using System.Net.Http.Headers;
using System.Reflection;
using System.Web;
using Newtonsoft.Json;
using WebSocketSharp;
using Mario.MongoDB;
using Mario.Setup;

namespace Mario.Quote
{
    public static class Get
    {
        public static WebSocket socketClient;

        public static async void run()
        {
            Request request = new Request();

            List<string> product_ids = new List<string>();

            foreach (KeyValuePair<string, Coin> entry in Global.list_coin_objs)
            {
                product_ids.Add(entry.Value.symbol);
            }

            request.product_ids = product_ids;

            JArray channels_arr = new JArray();
            channels_arr.Add("heartbeat");

            JObject o = JObject.FromObject(new
            {
                name = "ticker",
                product_ids = product_ids
            });
            channels_arr.Add(JObject.FromObject(o));

            request.channels = channels_arr;

            var req = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            socketClient = new WebSocket("wss://ws-feed.exchange.coinbase.com");

            if (Environment.OSVersion.Version.Major > 5)
            {
                socketClient.SslConfiguration.EnabledSslProtocols = (System.Security.Authentication.SslProtocols)3072;
                socketClient.SslConfiguration.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            }

            socketClient.OnError += (sender, e) =>
            {
                Console.WriteLine("ERROR: " + e.Exception.Message);
                socketClient.Close();
                Environment.Exit(0);
            };

            socketClient.OnMessage += (sender, e) =>
            {
                var jToken = JToken.Parse(e.Data);
                var payload_type = "";
                if ((string)jToken.SelectToken("type") == "ticker")
                {
                    var product_id = (string)jToken.SelectToken("product_id");
                    Coin product = Global.list_coin_objs[product_id];

                    var quoteDate = DateTime.Parse((string)jToken.SelectToken("time"), null, System.Globalization.DateTimeStyles.RoundtripKind);

                    Writer.quote_for_ticker(
                        product.collection_quote,
                        product_id,
                        quoteDate,
                        (decimal)jToken.SelectToken("price"),
                        (decimal)jToken.SelectToken("best_bid"),
                        (decimal)jToken.SelectToken("best_ask"),
                        (decimal)jToken.SelectToken("last_size"),
                        (string)jToken.SelectToken("side")
                        );
                }
            };

            socketClient.OnClose += (sender, e) =>
            {
                Console.WriteLine("Closed: " + e.Reason.ToString());

                Composer.start_trade();

                Composer.db_cleanse();

                Quote.Get.run();

                //Environment.Exit(0);
            };

            socketClient.OnOpen += (sender, e) =>
                Console.WriteLine("Opened:");

            socketClient.Connect();
            socketClient.Send(req);
        }
    }
}