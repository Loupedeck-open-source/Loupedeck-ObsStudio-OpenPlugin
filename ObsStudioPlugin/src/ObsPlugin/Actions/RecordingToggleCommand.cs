namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class RecordingToggleCommand : GenericOnOffSwitch
    {
        public RecordingToggleCommand()
                        : base(
                            name: "ToggleRecording",
                            displayName: "Recording Toggle",
                            description: "Toggles Recording on or off",
                            groupName: "",
                            offStateName: "Start Recording",
                            onStateName: "Stop Recording",
                            offStateImage: "STREAM_ToggleRecord1.png",
                            onStateImage: "STREAM_ToggleRecord2.png")
        {
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            ObsStudioPlugin.Proxy.AppEvtRecordingOff += eventSwitchedOn;
            ObsStudioPlugin.Proxy.AppEvtRecordingOn += eventSwitchedOff;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            ObsStudioPlugin.Proxy.AppEvtRecordingOff -= eventSwitchedOn;
            ObsStudioPlugin.Proxy.AppEvtRecordingOn -= eventSwitchedOff;
        }

        protected override void RunToggle() => ObsStudioPlugin.Proxy.AppToggleRecording();
    }
}
