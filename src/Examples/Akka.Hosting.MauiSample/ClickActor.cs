using Akka.Actor;
using Akka.Util.Internal;

namespace Akka.Hosting.MauiSample
{
    public class ClickActor: ReceiveActor
    {
        public static Props Props { get; } = Props.Create(() => new ClickActor());

        private readonly AtomicCounter _counter = new AtomicCounter(0);

        public ClickActor() 
        { 
            Receive<Click>(_ => 
            {
                var count = _counter.IncrementAndGet();
                var text = count == 1 ? $"Clicked {count} time" : $"Clicked {count} times";
                Sender.Tell(text);
            });
        }

    }
}
