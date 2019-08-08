using System;
using System.Collections.Generic;
using System.Text;

namespace IdleRPG.NET {
    public static class Config {
        public static int LimitPen = 604800;
        public static int RPItemBase = 600;
        public static int RPBase = 10; //600;
        public static float RPStep = 1.16f;
        public static float RPPenStep = 1.14f;
        public static int MapX = 500;
        public static int MapY = 500;
        public static string PrimNick = "IdleRPG";
        public static int Tick = 3;
        public static int QuestLevel = 1;
        public static int TournamentLevel = 1;
        public static string Server = "server.local";
        public static string ChannelName = "#IdleRPG";
        public static int Port = 6667;
        public static bool UseSSL = false;
        public static string Owner = "Syzygy";
        public static bool AutoLogin = true;
    }
}
