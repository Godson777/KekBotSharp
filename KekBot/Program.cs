using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using KekBot.Utils;

namespace KekBot {
    class Program {

        public static RethinkDB R = RethinkDB.R;
        public static Connection Conn;
        private static Dictionary<int, KekBot> Shards { get; set; }


        static void Main(string[] args) {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Running the bot requires knowledge of English")]
        static async Task MainAsync(string[] _) {
            Console.WriteLine("KekBot is starting...");
            Console.WriteLine("[1/3] Loading Config...");
            var config = await Config.Get();

            Console.WriteLine("[2/3] Connecting to RethinkDB...");
            try {
                Conn = R.Connection().User(config.DbUser, config.DbPass).Connect();
            } catch (Exception e) when (e is ReqlDriverError || e is System.Net.Sockets.SocketException) {
                Util.Panic("[RethinkDB] There was an error logging in, are you sure that RethinkDB is on, or that you typed the info correctly?");
            }
            Console.WriteLine("[RethinkDB] Connection Success!");

            Console.WriteLine("[RethinkDB] Checking Config for DB...");
            if (config.Db == null) {
                Util.Panic("[RethinkDB] There was no database to use provided. Make sure \"database\" is in your config.json.");
            }

            if (!(bool)R.DbList().Contains(config.Db).Run(Conn)) {
                R.DbCreate(config.Db).Run(Conn);
                Console.WriteLine("[RethinkDB] Database wasn't found, so it was created.");
            }
            Conn.Use(config.Db);
            Console.WriteLine("[RethinkDB] Connected to Database!");
            VerifyTables();

            Console.WriteLine("[3/3] Creating and booting shards...");
            Shards = new Dictionary<int, KekBot>();
            for (int i = 0; i < config.Shards; i++) {
                Shards[i] = new KekBot(config, i);
            }

            Console.WriteLine("Loading Completed! Booting shards!");
            Console.WriteLine("----------------------------------");

            foreach (var (_, shard) in Shards) {
                await shard.StartAsync();
            }

            GC.Collect();

            await Task.Delay(-1);
        }


        /// <summary>
        /// Verifies if the all the tables exist in our database.
        /// </summary>
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "shut your whore mouth")]
        private static void VerifyTables() {
            Console.WriteLine("[RethinkDB] Verifying that all required tables have been created...");
            if (!R.TableList().Contains("Profiles").Run<bool>(Conn)) {
                Console.WriteLine("[RethinkDB] \"Profiles\" table was not found, so it is being made.");
                R.TableCreate("Profiles").OptArg("primary_key", "User ID").Run(Conn);
            }
            if (!R.TableList().Contains("Responses").Run<bool>(Conn)) {
                Console.WriteLine("[RethinkDB] \"Responses\" table was not found, so it is being made.");
                R.TableCreate("Responses").OptArg("primary_key", "Action").Run(Conn);
            }
            if (!R.TableList().Contains("Settings").Run<bool>(Conn)) {
                Console.WriteLine("[RethinkDB] \"Settings\" table was not found, so it is being made.");
                R.TableCreate("Settings").OptArg("primary_key", "Guild ID").Run(Conn);
            }
            if (!R.TableList().Contains("Takeovers").Run<bool>(Conn)) {
                Console.WriteLine("[RethinkDB] \"Takeovers\" table was not found, so it is being made.");
                R.TableCreate("Takeovers").OptArg("primary_key", "Name").Run(Conn);
            }
            if (!R.TableList().Contains("Tickets").Run<bool>(Conn)) {
                Console.WriteLine("[RethinkDB] \"Tickets\" table was not found, so it is being made.");
                R.TableCreate("Tickets").OptArg("primary_key", "ID").Run(Conn);
            }
            if (!R.TableList().Contains("Twitter").Run<bool>(Conn)) {
                Console.WriteLine("[RethinkDB] \"Twitter\" table was not found, so it is being made.");
                R.TableCreate("Twitter").OptArg("primary_key", "Account ID").Run(Conn);
            }
            Console.WriteLine("[RethinkDB] Tables verified!");
        }
        /*
         * @todo Adjust collecting resources like config.json and others.
         * @body This needs to be edited later, either the location of all the resources should be a commandline argument, or we simply have a commandline argument named --dev or something to differentiate the environment KekBot is in. That way we can test but still have all the proper resources and stuff where they need to be.
         */

    }
}
