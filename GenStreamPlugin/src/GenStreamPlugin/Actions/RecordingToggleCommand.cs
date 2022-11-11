namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;

    public class RecordingToggleCommand : GenericOnOffSwitch
    {
        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;

        public RecordingToggleCommand()
                : base("Toggles Recording", "Toggles Recording on or off", /*no group*/"",
                new String[] {
                    "Command unavailable",
                    "Start Recording",
                    "Stop Recording"
                },
                new String[] {
                  "Loupedeck.GenStreamPlugin.icons.SoftwareNotFound.png",
                  "Loupedeck.GenStreamPlugin.icons.STREAM_ToggleRecord2.png",
                  "Loupedeck.GenStreamPlugin.icons.STREAM_ToggleRecord1.png"
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
