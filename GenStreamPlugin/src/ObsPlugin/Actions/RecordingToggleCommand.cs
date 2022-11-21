namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    public class RecordingToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;

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
            this.Proxy.AppEvtRecordingOff += eventSwitchedOn;
            this.Proxy.AppEvtRecordingOn += eventSwitchedOff;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            this.Proxy.AppEvtRecordingOff -= eventSwitchedOn;
            this.Proxy.AppEvtRecordingOn -= eventSwitchedOff;
        }

        protected override void RunToggle() => this.Proxy.AppToggleRecording();
    }
}
