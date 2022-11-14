namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class RecordingToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        public RecordingToggleCommand()
                        : base(displayName: "Recording Toggle", 
                       description: "Toggles Recording on or off", 
                       groupName:      "",
                       offStateName:   "Start Recording",
                       onStateName:    "Stop Recording",
                       offStateImage:  "Loupedeck.ObsPlugin.icons.STREAM_ToggleRecord1.png",
                       onStateImage:   "Loupedeck.ObsPlugin.icons.animated-record.gif")
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
