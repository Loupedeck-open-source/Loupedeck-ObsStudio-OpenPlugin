namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class StreamingToggleCommand : GenericOnOffSwitch
    {
        // TODO: As needed, add handling for Starting and Stopping states
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
            ObsStudioPlugin.Proxy.AppEvtStreamingOff += eventSwitchedOn;
            ObsStudioPlugin.Proxy.AppEvtStreamingOn += eventSwitchedOff;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            ObsStudioPlugin.Proxy.AppEvtStreamingOff -= eventSwitchedOn;
            ObsStudioPlugin.Proxy.AppEvtStreamingOn -= eventSwitchedOff;
        }

        protected override void RunCommand(TwoStateCommand command)
        {
            switch (command)
            {
                case TwoStateCommand.TurnOff:
                    ObsStudioPlugin.Proxy.AppStopStreaming();
                    break;

                case TwoStateCommand.TurnOn:
                    ObsStudioPlugin.Proxy.AppStartStreaming();
                    break;

                case TwoStateCommand.Toggle:
                    ObsStudioPlugin.Proxy.AppToggleStreaming();
                    break;
            }
        }
    }
}
