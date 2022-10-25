namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;

    class VirtualCameraCommand : GenericOnOffSwitch
    {
        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;

        public VirtualCameraCommand() 
                : base("Virtual Camera", "Toggles Virtual Camera on or off", /*no group*/"", 
                new String[] {  
                    "Command unavailable", 
                    "Start Virtual Camera",
                    "Stop Virtual Camera"
                },
                new String[] {
                  "Loupedeck.GenStreamPlugin.icons.SoftwareNotFound.png",
                  "Loupedeck.GenStreamPlugin.icons.VirtualWebcam.png",
                  "Loupedeck.GenStreamPlugin.icons.VirtualWebcamOff.png"
                })
        {

        }
        protected override void ConnectAppEvents(EventHandler<EventArgs> OnEvent, EventHandler<EventArgs> OffEvent)
        {
            this.Proxy.AppEvtVirtualCamOff += OffEvent;
            this.Proxy.AppEvtVirtualCamOn += OnEvent;
        }
        protected override void DisconnectAppEvents(EventHandler<EventArgs> OnEvent, EventHandler<EventArgs> OffEvent)
        {
            this.Proxy.AppEvtVirtualCamOff += OffEvent;
            this.Proxy.AppEvtVirtualCamOn += OnEvent;
        }

        protected override void RunCommand(String actionParameter) => this.Proxy.AppToggleVirtualCam();
    }
}
