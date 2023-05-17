using Akka.Actor;

namespace Akka.Hosting.Maui
{
    public partial class MainPage : ContentPage
    {
        private readonly IActorRef _echo;

        public MainPage(ActorRegistry registry)
        {
            _echo = registry.Get<ClickActor>();
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            _echo.Ask<string>(Click.Instance, TimeSpan.FromSeconds(5))
                .ContinueWith(async t =>
                {
                    var text = t.IsCanceled ? "CANCELED" : t.IsFaulted ? t.Exception!.Message : t.Result;

                    await CounterBtn.Dispatcher.DispatchAsync(() =>
                    {
                        CounterBtn.Text = text;
                    });
                });
        }
    }
}