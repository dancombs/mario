using Coinbase.Pro;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Net.Http;
using Skender.Stock.Indicators;

namespace Mario
{
    public class Global
    {
        public static string directory_exe = "";
        public static string directory_sound = "";
        public static string file_sound_player = "";

        public static CoinbaseProClient coinbase_client = null;

        public static string coin_list = "";
        public static IDictionary<string, Coin> list_coin_objs = null;

        public static string mongodb_url = "";
        public static MongoClient mongodb_client;
        public static IMongoDatabase mongodb_database;

        public static string db_name = "mario_v1";
        public static string db_app_settings = "0_app_settings";
        public static string db_trade_settings = "0_trade_settings";
        public static string db_trade_log = "0_trade_log";
        public static string db_collection_prefix = "x_";
        public static string db_collection_quote = "_quote";
        public static string db_collection_study = "_study";
        public static string db_collection_total = "_total";

        public static IMongoCollection<BsonDocument> collection_app_settings;
        public static IMongoCollection<BsonDocument> collection_trade_settings;
        public static IMongoCollection<BsonDocument> collection_trade_log;

        public static FilterDefinition<BsonDocument> filter_empty;
    }
}