using System.Collections.Generic;
using System.IO;

namespace IdleRPG.NET {
    public static class Utilities {
        public static List<Event> LoadEvents() {
            if (!File.Exists("events.txt"))
                throw new FileNotFoundException("Cannot find the file \"events.txt\".");

            List<Event> events = new List<Event>();

            using (StreamReader sr = File.OpenText("events.txt")) {
                string eventText = string.Empty;
                while ((eventText = sr.ReadLine()) != null) {
                    if (eventText.Substring(0, 1) == "C") {
                        events.Add(new Event() { EventType = EventType.Calamity, EventText = eventText.Substring(2) });
                    } else if (eventText.Substring(0, 1) == "G") {
                        events.Add(new Event() { EventType = EventType.Godsend, EventText = eventText.Substring(2) });
                    } else if (eventText.Substring(0, 1) == "Q") {
                        if (eventText.Substring(0, 2) == "Q1") {
                            events.Add(new Event() { EventType = EventType.Quest1, EventText = eventText.Substring(3) });
                        } else if (eventText.Substring(0, 2) == "Q2") {
                            events.Add(new Event() { EventType = EventType.Quest2, EventText = eventText.Substring(3) });
                        }
                    }
                }
            }

            return events;
        }
    }
}
