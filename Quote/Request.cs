using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mario.Quote
{
    class Request
    {
        public string type = "subscribe";
        public string ApiKey = Global.coinbase_client.Config.ApiKey;
        public string Secret = Global.coinbase_client.Config.Secret;
        public string Passphrase = Global.coinbase_client.Config.Passphrase;

        public List<string> product_ids { get; set; }
        public JArray channels { get; set; }
    }
}