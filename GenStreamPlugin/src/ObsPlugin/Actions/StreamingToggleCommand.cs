namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class StreamingToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        public StreamingToggleCommand()
                     : base(name: "ToggleStreaming", 
                    displayName: "Streaming Toggle",
                    description: "Toggles Streaming on or off",
                    groupName: "",
                    offStateName: "Start streaming",
                    onStateName: "Stop streaming",
                    offStateImage: "STREAM_StartStreamingGreen.png",
                    onStateImage: "animated-streaming.gif")
        {
        }


        protected override void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            this.Proxy.AppEvtStreamingOff += eventSwitchedOn;
            this.Proxy.AppEvtStreamingOn += eventSwitchedOff;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            this.Proxy.AppEvtStreamingOff -= eventSwitchedOn;
            this.Proxy.AppEvtStreamingOn -= eventSwitchedOff;
        }

        protected override void RunToggle() => this.Proxy.AppToggleStreaming();
    }
}
