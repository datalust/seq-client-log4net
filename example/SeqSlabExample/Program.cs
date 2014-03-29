using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Seq;

namespace SeqSlabExample
{
    [EventSource(Name = "Sample")]
    public class SampleEventSource : EventSource
    {
        public class Keywords
        {
            public const EventKeywords SampleApp = (EventKeywords)1;
        }

        public class Tasks
        {
            public const EventTask Run = (EventTask)1;
        }

        static readonly SampleEventSource _log = new SampleEventSource();

        private SampleEventSource() { }
        public static SampleEventSource Log { get { return _log; } }

        [Event(1, Message = "Hello, {0}",
        Level = EventLevel.Informational, Keywords = Keywords.SampleApp, Task = Tasks.Run)]
        internal void Greeting(string name)
        {
            WriteEvent(1, name);
        }
    }

    class Program
    {
        static void Main()
        {
            using (var listener = new ObservableEventListener())
            {
                listener.EnableEvents(SampleEventSource.Log, EventLevel.LogAlways, SampleEventSource.Keywords.SampleApp);

                listener.LogToConsole();
                listener.LogToSeq("http://my-seq");

                SampleEventSource.Log.Greeting(Environment.UserName);
            }

            Console.WriteLine("Done");
            Console.ReadKey(true);
        }
    }
}
