using IdleRPG.NET;
using IrcDotNet;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace IdleRPG.NET_Bot {
    class Program {
        private static World world;
        static void Main(string[] args) {
            using (var client = new StandardIrcClient()) {
                client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
                client.Disconnected += Client_Disconnected;
                client.Registered += Client_Registered;

                using (var registeredEvent = new ManualResetEventSlim(false)) {
                    using (var connectedEvent = new ManualResetEventSlim(false)) {
                        client.Connected += (sender2, e2) => connectedEvent.Set();
                        client.Registered += (sender2, e2) => registeredEvent.Set();
                        client.Connect(Config.Server, Config.Port, Config.UseSSL, new IrcUserRegistrationInfo()
                        {
                            NickName = Config.PrimNick,
                            RealName = Config.PrimNick,
                            UserName = Config.PrimNick,
                            Password = "Crt25890",
                            UserModes = new List<char>()
                            {
                                'i',
                                'x'
                            }
                        });
                        if (!connectedEvent.Wait(10000)) {
                            Console.WriteLine($"Connection to '{Config.Server}' timed out.");
                            return;
                        }
                    }
                    Console.WriteLine($"Now connected to '{Config.Server}'.");
                    if (!registeredEvent.Wait(10000)) {
                        Console.WriteLine($"Could not register to '{Config.Server}'.");
                        return;
                    }
                }

                Console.WriteLine($"Now registered to '{Config.Server}' as '{Config.PrimNick}'.");
                HandleEventLoop(client);
            }
        }

        private static void HandleEventLoop(IrcClient client) {
            world = new World(client);
            client.Channels.Join("#IdleRPG");

            while (world.Running) {
                if ((int)(DateTime.Now - world.LastTime).TotalSeconds >= Config.Tick)
                    world.RPCheck();
                else
                    Thread.Sleep(Config.Tick * 1000);
            }

            client.Disconnect();
        }

        private static void Client_Registered(object sender, EventArgs e) {
            var client = (IrcClient)sender;

            client.LocalUser.NoticeReceived += LocalUser_NoticeReceived;
            client.LocalUser.MessageReceived += LocalUser_MessageReceived;
            client.LocalUser.JoinedChannel += LocalUser_JoinedChannel;
            client.LocalUser.LeftChannel += LocalUser_LeftChannel;

            client.SendRawMessage("privmsg NickServ identify Crt25890");
        }

        private static void LocalUser_LeftChannel(object sender, IrcChannelEventArgs e) {
            var localUser = (IrcLocalUser)sender;

            e.Channel.UserJoined -= Channel_UserJoined;
            e.Channel.UserLeft -= Channel_UserLeft;
            e.Channel.MessageReceived -= Channel_MessageReceived;
            e.Channel.NoticeReceived -= Channel_NoticeReceived;
            e.Channel.UserKicked -= Channel_UserKicked;
            e.Channel.UsersListReceived -= Channel_UsersListReceived;

            Console.WriteLine($"You left the channel {e.Channel.Name}.");
        }

        private static void LocalUser_JoinedChannel(object sender, IrcChannelEventArgs e) {
            var localUser = (IrcLocalUser)sender;

            e.Channel.UserJoined += Channel_UserJoined;
            e.Channel.UserLeft += Channel_UserLeft;
            e.Channel.MessageReceived += Channel_MessageReceived;
            e.Channel.NoticeReceived += Channel_NoticeReceived;
            e.Channel.UserKicked += Channel_UserKicked;
            e.Channel.UsersListReceived += Channel_UsersListReceived;

            Console.WriteLine($"You joined the channel {e.Channel.Name}.");
            e.Channel.Client.SendRawMessage("privmsg ChanServ op #IdleRPG IdleRPG");
        }

        private static void Channel_UsersListReceived(object sender, EventArgs e) {
            world.Start();
        }

        private static void LocalUser_MessageReceived(object sender, IrcMessageEventArgs e) {
            var localUser = (IrcLocalUser)sender;
            if (e.Source is IrcUser ircUser) {
                world.ParseMessage(ircUser, e.Text);
                // Read message.
                Console.WriteLine("({0}): {1}.", e.Source.Name, e.Text);
            } else {
                Console.WriteLine("({0}) Message: {1}.", e.Source.Name, e.Text);
            }
        }

        private static void LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e) {
            var localUser = (IrcLocalUser)sender;
            Console.WriteLine("Notice: {0}.", e.Text);
        }

        private static void Channel_UserKicked(object sender, IrcChannelUserEventArgs e) {
            var channel = (IrcChannel)sender;
            world.Penalize(e.ChannelUser.User.NickName, "kick");
            Console.WriteLine("[{0}] User {1} was kicked from the channel.", channel.Name, e.ChannelUser.User.NickName);
        }

        private static void Channel_NoticeReceived(object sender, IrcMessageEventArgs e) {
            var channel = (IrcChannel)sender;
            Console.WriteLine("[{0}] Notice: {1}.", channel.Name, e.Text);
        }

        private static void Channel_MessageReceived(object sender, IrcMessageEventArgs e) {
            var channel = (IrcChannel)sender;
            if (e.Source is IrcUser ircUser) {
                world.Penalize(ircUser.NickName, "msg", e.Text.Length);
                // Read message.
                Console.WriteLine("[{0}]({1}): {2}.", channel.Name, e.Source.Name, e.Text);
            } else {
                Console.WriteLine("[{0}]({1}) Message: {2}.", channel.Name, e.Source.Name, e.Text);
            }
        }

        private static void Channel_UserLeft(object sender, IrcChannelUserEventArgs e) {
            var channel = (IrcChannel)sender;
            world.Penalize(e.ChannelUser.User.NickName, "part");
            Console.WriteLine("[{0}] User {1} left the channel.", channel.Name, e.ChannelUser.User.NickName);
        }

        private static void Channel_UserJoined(object sender, IrcChannelUserEventArgs e) {
            var channel = (IrcChannel)sender;
            e.ChannelUser.User.NickNameChanged += User_NickNameChanged;
            Console.WriteLine("[{0}] User {1} joined the channel.", channel.Name, e.ChannelUser.User.NickName);
        }

        private static void User_NickNameChanged(object sender, EventArgs e) {
            world.Penalize("test", "nick");
        }

        private static void Client_Disconnected(object sender, EventArgs e) {
            var client = (IrcClient)sender;
        }
    }
}
