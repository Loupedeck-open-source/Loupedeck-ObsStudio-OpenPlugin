namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;

    public class VirtualCameraToggleCommand : GenericOnOffSwitch
    {
        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;

        public VirtualCameraToggleCommand()
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

        protected override void ConnectAppEvents(EventHandler<EventArgs> onEvent, EventHandler<EventArgs> offEvent)
        {
            this.Proxy.AppEvtVirtualCamOff += offEvent;
            this.Proxy.AppEvtVirtualCamOn += onEvent;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> onEvent, EventHandler<EventArgs> offEvent)
        {
            this.Proxy.AppEvtVirtualCamOff -= offEvent;
            this.Proxy.AppEvtVirtualCamOn -= onEvent;
        }

        protected override void RunCommand(String actionParameter) => this.Proxy.AppToggleVirtualCam();
    }
}
