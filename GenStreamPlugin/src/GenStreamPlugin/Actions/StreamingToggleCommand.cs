namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;

    public class StreamingToggleCommand : GenericOnOffSwitch
    {
        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;

        public StreamingToggleCommand()
                : base("Toggles Streaming", "Toggles Streaming on or off", /*no group*/"",
                new String[] {
                    "Command unavailable",
                    "Toggle On",
                    "Toggle Off"
                },
                new String[] {
                  "Loupedeck.GenStreamPlugin.icons.SoftwareNotFound.png",
                  "Loupedeck.GenStreamPlugin.icons.STREAM_StartStreamingGreen.png",
                  "Loupedeck.GenStreamPlugin.icons.STREAM_StopStreamingRed.png"
                })
        {
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> onEvent, EventHandler<EventArgs> offEvent)
        {
            this.Proxy.AppEvtStreamingOff += offEvent;
            this.Proxy.AppEvtStreamingOn += onEvent;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> onEvent, EventHandler<EventArgs> offEvent)
        {
            this.Proxy.AppEvtStreamingOff -= offEvent;
            this.Proxy.AppEvtStreamingOn -= onEvent;
        }

        protected override void RunCommand(String actionParameter) => this.Proxy.AppToggleStreaming();
    }
}
