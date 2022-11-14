namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class VirtualCameraToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        public VirtualCameraToggleCommand()
                  : base(displayName: "Virtual Camera Toggle",
                    description: "Toggles Virtual Camera on or off",
                    groupName: "",
                    offStateName: "Start Virtual Camera",
                    onStateName: "Stop Virtual Camera",
                    offStateImage: "Loupedeck.ObsPlugin.icons.VirtualWebcam.png",
                    onStateImage: "Loupedeck.ObsPlugin.icons.animated-camera.gif")
        {
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            this.Proxy.AppEvtVirtualCamOff += eventSwitchedOn;
            this.Proxy.AppEvtVirtualCamOn += eventSwitchedOff;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            this.Proxy.AppEvtVirtualCamOff -= eventSwitchedOn;
            this.Proxy.AppEvtVirtualCamOn -= eventSwitchedOff;
        }

        protected override void RunToggle() => this.Proxy.AppToggleVirtualCam();

    }
}
