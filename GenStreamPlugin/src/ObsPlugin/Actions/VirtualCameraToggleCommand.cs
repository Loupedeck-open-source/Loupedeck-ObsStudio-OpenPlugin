namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    public class VirtualCameraToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;

        public VirtualCameraToggleCommand()
                  : base(name: "VirtualCam", 
                    displayName: "Virtual Camera Toggle",
                    description: "Toggles Virtual Camera on or off",
                    groupName: "",
                    offStateName: "Start Virtual Camera",
                    onStateName: "Stop Virtual Camera",
                    offStateImage: "VirtualWebcam.png",
                    onStateImage: "animated-camera.gif")
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
