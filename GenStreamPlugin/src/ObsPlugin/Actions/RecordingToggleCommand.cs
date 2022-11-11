namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class RecordingToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        public RecordingToggleCommand()
                : base("Recording Toggle", "Toggles Recording on or off", /*no group*/"",
                new String[] {
                    "Command unavailable",
                    "Start Recording",
                    "Stop Recording"
                },
                new String[] {
                  "Loupedeck.ObsPlugin.icons.SoftwareNotFound.png",
                  "Loupedeck.ObsPlugin.icons.STREAM_ToggleRecord2.png",
                  "Loupedeck.ObsPlugin.icons.STREAM_ToggleRecord1.png"
                })
        {
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> onEvent, EventHandler<EventArgs> offEvent)
        {
            this.Proxy.AppEvtRecordingOff += offEvent;
            this.Proxy.AppEvtRecordingOn += onEvent;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> onEvent, EventHandler<EventArgs> offEvent)
        {
            this.Proxy.AppEvtRecordingOff -= offEvent;
            this.Proxy.AppEvtRecordingOn -= onEvent;
        }

        protected override void RunCommand(String actionParameter) => this.Proxy.AppToggleRecording();
    }
}
