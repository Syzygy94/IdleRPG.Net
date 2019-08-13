using System.Configuration;

namespace IdleRPG.NET {
    public static class Config {
        public static int LimitPen { get { return int.Parse(ConfigurationManager.AppSettings["LimitPen"]); } }
        public static int RPItemBase { get { return int.Parse(ConfigurationManager.AppSettings["RPItemBase"]); } }
        public static int RPBase { get { return int.Parse(ConfigurationManager.AppSettings["RPBase"]); } }
        public static float RPStep { get { return float.Parse(ConfigurationManager.AppSettings["RPStep"]); } }
        public static float RPPenStep { get { return float.Parse(ConfigurationManager.AppSettings["RPPenStep"]); } }
        public static int MapX { get { return int.Parse(ConfigurationManager.AppSettings["MapX"]); } }
        public static int MapY { get { return int.Parse(ConfigurationManager.AppSettings["MapY"]); } }
        public static string PrimNick { get { return ConfigurationManager.AppSettings["PrimNick"]; } }
        public static string Password { get { return ConfigurationManager.AppSettings["Password"]; } }
        public static int Tick { get { return int.Parse(ConfigurationManager.AppSettings["Tick"]); } }
        public static int QuestLevel { get { return int.Parse(ConfigurationManager.AppSettings["QuestLevel"]); } }
        public static int TournamentLevel { get { return int.Parse(ConfigurationManager.AppSettings["TournamentLevel"]); } }
        public static string Server { get { return ConfigurationManager.AppSettings["Server"]; } }
        public static string ChannelName { get { return ConfigurationManager.AppSettings["ChannelName"]; } }
        public static int Port { get { return int.Parse(ConfigurationManager.AppSettings["Port"]); } }
        public static bool UseSSL { get { return bool.Parse(ConfigurationManager.AppSettings["UseSSL"]); } }
        public static string Owner { get { return ConfigurationManager.AppSettings["Owner"]; } }
        public static bool AutoLogin { get { return bool.Parse(ConfigurationManager.AppSettings["AutoLogin"]); } }
        public static string CryptoVector { get { return ConfigurationManager.AppSettings["CryptoVector"]; } }
        public static string CryptoKey { get { return ConfigurationManager.AppSettings["CryptoKey"]; } }
    }
}
