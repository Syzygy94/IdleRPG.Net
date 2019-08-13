namespace IdleRPG.NET {
    public enum EventType {
        Calamity,
        Godsend,
        Quest1,
        Quest2
    }

    public class Event {
        public EventType EventType { get; set; }
        public string EventText { get; set; }
    }
}
