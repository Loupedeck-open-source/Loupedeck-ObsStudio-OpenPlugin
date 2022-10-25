namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;

    class RecordingCommand : GenericOnOffSwitch
    {
        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;

        public RecordingCommand() 
                : base("Toggles Recording", "Toggles Recording on or off", /*no group*/"", 
                new String[] {  
                    "Command unavailable", 
                    "Start Recording", 
                    "Stop Recording"
                },
                new String[] {
                  "Loupedeck.GenStreamPlugin.icons.SoftwareNotFound.png",
                  "Loupedeck.GenStreamPlugin.icons.STREAM_ToggleRecord1.png",
                  "Loupedeck.GenStreamPlugin.icons.STREAM_ToggleRecord2.png"
                })
        {

        }
        protected override void ConnectAppEvents(EventHandler<EventArgs> OnEvent, EventHandler<EventArgs> OffEvent)
        {
            this.Proxy.AppEvtRecordingOff += OffEvent;
            this.Proxy.AppEvtRecordingOn += OnEvent;
        }
        protected override void DisconnectAppEvents(EventHandler<EventArgs> OnEvent, EventHandler<EventArgs> OffEvent)
        {
            this.Proxy.AppEvtRecordingOff += OffEvent;
            this.Proxy.AppEvtRecordingOn += OnEvent;
        }
        protected override void RunCommand(String actionParameter) => this.Proxy.AppToggleRecording();

    }
}
