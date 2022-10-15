namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;

    class StreamingCommand : GenericOnOffSwitch
    {
        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;

        public StreamingCommand() 
                : base("Toggles Streaming", "Toggles Streaming on or off", /*no group*/"", 
                new String[] {  
                    "Command unavailable", 
                    "Toggle On", 
                    "Toggle Off"
                },
                new String[] { 
                  "Loupedeck.GenStreamPlugin.icons.Toggle1Disabled.png",
                  "Loupedeck.GenStreamPlugin.icons.Toggle1Off.png",
                  "Loupedeck.GenStreamPlugin.icons.Toggle1On.png"
                })
        {

        }
        protected override void ConnectAppEvents(EventHandler<EventArgs> OnEvent, EventHandler<EventArgs> OffEvent)
        {
            this.Proxy.AppEvtStreamingOff += OffEvent;
            this.Proxy.AppEvtStreamingOn += OnEvent;
        }
        protected override void DisconnectAppEvents(EventHandler<EventArgs> OnEvent, EventHandler<EventArgs> OffEvent)
        {
            this.Proxy.AppEvtStreamingOff += OffEvent;
            this.Proxy.AppEvtStreamingOn += OnEvent;
        }
        protected override void RunCommand(String actionParameter) => this.Proxy.AppToggleStreaming();

    }
}
