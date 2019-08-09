using IdleRPG.NET;
using Meebey.SmartIrc4net;
using System;
using System.Collections;
using System.Threading;

namespace IdleRPG.NET_Bot {
    class Program {
        private static World world;
        private static IrcClient ircClient = new IrcClient();

        static void Main(string[] args) {
            ircClient.SendDelay = 200;
            ircClient.ActiveChannelSyncing = true;
            ircClient.UseSsl = Config.UseSSL;

            ircClient.OnConnected += IrcClient_OnConnected;
            ircClient.OnQueryNotice += IrcClient_OnQueryNotice;
            ircClient.OnError += IrcClient_OnError;

            try {
                // here we try to connect to the server and exceptions get handled
                ircClient.Connect(Config.Server, Config.Port);
            } catch (ConnectionException e) {
                // something went wrong, the reason will be shown
                Console.WriteLine("Couldn't connect! Reason: " + e.Message);
                Exit();
            }

            try {
                ircClient.Login(Config.PrimNick, Config.PrimNick);
                ircClient.RfcJoin(Config.ChannelName);
                new Thread(new ThreadStart(ReadCommands)).Start();
                ircClient.Listen();
                ircClient.Disconnect();
            } catch (ConnectionException) {
                // this exception is handled because Disconnect() can throw a not
                // connected exception
                Exit();
            } catch (Exception e) {
                // this should not happen by just in case we handle it nicely
                Console.WriteLine("Error occurred! Message: " + e.Message);
                Console.WriteLine("Exception: " + e.StackTrace);
                Exit();
            }
        }

        private static void IrcClient_OnConnected(object sender, EventArgs e) {
            ircClient.OnChannelActiveSynced += IrcClient_OnChannelActiveSynced;
            ircClient.OnChannelMessage += IrcClient_OnChannelMessage;
            ircClient.OnPart += IrcClient_OnPart;
            ircClient.OnQueryMessage += IrcClient_OnQueryMessage;
            ircClient.OnKick += IrcClient_OnKick;
            ircClient.OnNickChange += IrcClient_OnNickChange;
            ircClient.OnQuit += IrcClient_OnQuit;
            world = new World(ircClient);

            ircClient.SendMessage(SendType.Message, "NickServ", $"Identify {Config.Password}", Priority.High);
        }

        private static void IrcClient_OnQueryNotice(object sender, IrcEventArgs e) {
            Console.WriteLine($"Notice: {e.Data.Message}");
        }

        private static void IrcClient_OnError(object sender, ErrorEventArgs e) {
            Console.WriteLine("Error: " + e.ErrorMessage);
            Exit();
        }

        private static void IrcClient_OnQueryMessage(object sender, IrcEventArgs e) {
            world.ParseMessage(ircClient.GetIrcUser(e.Data.Nick), e.Data.MessageArray);
        }

        private static void IrcClient_OnChannelMessage(object sender, IrcEventArgs e) {
            Hashtable penalty = new Hashtable
            {
                { "type", "msg" },
                { "nick", e.Data.Nick },
                { "textLength", e.Data.Message.Length }
            };
            world.Penalize(penalty);
        }

        private static void IrcClient_OnPart(object sender, PartEventArgs e) {
            Hashtable penalty = new Hashtable
            {
                { "type", "part" },
                { "nick", e.Who }
            };
            world.Penalize(penalty);
        }

        private static void IrcClient_OnChannelActiveSynced(object sender, IrcEventArgs e) {
            ircClient.RfcMode(Config.ChannelName, "+m");
            new Thread(new ThreadStart(StartRPG)).Start();
        }

        private static void IrcClient_OnQuit(object sender, QuitEventArgs e) {
            Hashtable penalty = new Hashtable
            {
                { "type", "quit" },
                { "nick", e.Who }
            };
            world.Penalize(penalty);
        }

        private static void IrcClient_OnNickChange(object sender, NickChangeEventArgs e) {
            Hashtable penalty = new Hashtable
            {
                { "type", "nick" },
                { "nick", e.OldNickname },
                { "newNick", e.NewNickname },
                { "host", e.Data.Host }
            };
            world.Penalize(penalty);
        }

        private static void IrcClient_OnKick(object sender, KickEventArgs e) {
            Hashtable penalty = new Hashtable
            {
                { "type", "kick" },
                { "nick", e.Whom }
            };
            world.Penalize(penalty);
        }

        public static void StartRPG() {
            world.Start();
            world.AutoLogin();
            while (world.Running) {
                if ((int)(DateTime.Now - world.LastTime).TotalSeconds >= Config.Tick)
                    world.RPCheck();
                else
                    Thread.Sleep(Config.Tick * 1000);
            }
        }

        public static void ReadCommands() {
            // here we read the commands from the stdin and send it to the IRC API
            // WARNING, it uses WriteLine() means you need to enter RFC commands
            // like "JOIN #test" and then "PRIVMSG #test :hello to you"
            while (true) {
                string cmd = System.Console.ReadLine();
                ircClient.WriteLine(cmd);
            }
        }

        public static void Exit() {
            // we are done, lets exit...
            Console.WriteLine("Exiting...");
            Environment.Exit(0);
        }
    }
}
