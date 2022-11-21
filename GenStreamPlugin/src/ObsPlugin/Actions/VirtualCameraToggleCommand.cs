namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    public class VirtualCameraToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;

        public VirtualCameraToggleCommand()
                  : base(
                      name: "VirtualCam",
                      displayName: "Virtual Camera Toggle",
                      description: "Switches the OBS Virtual Camera on/off",
                      groupName: "",
                      offStateName: "Start Virtual Camera",
                      onStateName: "Stop Virtual Camera",
                      offStateImage: "VirtualWebcamOff.png",
                      onStateImage: "VirtualWebcam.png")
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
