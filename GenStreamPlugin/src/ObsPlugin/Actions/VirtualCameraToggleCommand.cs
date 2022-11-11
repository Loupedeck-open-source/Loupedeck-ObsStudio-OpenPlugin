namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class VirtualCameraToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        public VirtualCameraToggleCommand()
                : base("Virtual Camera toggle", "Toggles Virtual Camera on or off", /*no group*/"",
                new String[] {
                    "Command unavailable",
                    "Start Virtual Camera",
                    "Stop Virtual Camera"
                },
                new String[] {
                  "Loupedeck.ObsPlugin.icons.SoftwareNotFound.png",
                  "Loupedeck.ObsPlugin.icons.VirtualWebcam.png",
                  "Loupedeck.ObsPlugin.icons.VirtualWebcamOff.png"
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
