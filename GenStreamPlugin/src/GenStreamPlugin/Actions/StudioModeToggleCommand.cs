namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;

    class StudioModeToggleCommand : GenericOnOffSwitch
    {
        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;

        public StudioModeToggleCommand() 
                : base("Studio Mode", "Toggles Studio Mode on or off", /*no group*/"", 
                new String[] {  
                    "Command unavailable", 
                    "Toggle On", 
                    "Toggle Off"
                },
                new String[] {
                  "Loupedeck.GenStreamPlugin.icons.SoftwareNotFound.png",
                  "Loupedeck.GenStreamPlugin.icons.STREAM_EnableStudioMode.png",
                  "Loupedeck.GenStreamPlugin.icons.STREAM_DisableStudioMode2.png"
                })
        {

        }
        protected override void ConnectAppEvents(EventHandler<EventArgs> OnEvent, EventHandler<EventArgs> OffEvent)
        {
            this.Proxy.AppEvtStudioModeOff += OffEvent;
            this.Proxy.AppEvtStudioModeOn += OnEvent;
        }
        protected override void DisconnectAppEvents(EventHandler<EventArgs> OnEvent, EventHandler<EventArgs> OffEvent)
        {
            this.Proxy.AppEvtStudioModeOff -= OffEvent;
            this.Proxy.AppEvtStudioModeOn -= OnEvent;
        }

        protected override void RunCommand(String actionParameter) => this.Proxy.AppToggleStudioMode();

    }
}
