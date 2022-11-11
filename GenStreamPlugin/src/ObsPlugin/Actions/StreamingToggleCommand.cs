namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class StreamingToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        public StreamingToggleCommand()
                : base("Toggle Streaming", "Toggles Streaming on or off", /*no group*/"",
                new String[] {
                    "Command unavailable",
                    "Toggle On",
                    "Toggle Off"
                },
                new String[] {
                  "Loupedeck.ObsPlugin.icons.SoftwareNotFound.png",
                  "Loupedeck.ObsPlugin.icons.STREAM_StartStreamingGreen.png",
                  "Loupedeck.ObsPlugin.icons.STREAM_StopStreamingRed.png"
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
