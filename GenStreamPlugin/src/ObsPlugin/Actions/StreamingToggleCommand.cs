namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    public class StreamingToggleCommand : GenericOnOffSwitch
    {
        // TODO: As needed, add handling for Starting and Stopping states
        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;

        public StreamingToggleCommand()
                     : base(
                         name: "ToggleStreaming",
                         displayName: "Streaming Toggle",
                         description: "Starts/Stops a livestream in OBS Studio",
                         groupName: "",
                         offStateName: "Start streaming",
                         onStateName: "Stop streaming",
                         offStateImage: "STREAM_StartStreamingGreen.png",
                         onStateImage:  "STREAM_StartStreamingRed.png")
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
