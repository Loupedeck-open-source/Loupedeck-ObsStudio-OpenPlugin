namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class VirtualCameraToggleCommand : GenericOnOffSwitch
    {

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
            ObsStudioPlugin.Proxy.AppEvtVirtualCamOff += eventSwitchedOn;
            ObsStudioPlugin.Proxy.AppEvtVirtualCamOn += eventSwitchedOff;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            ObsStudioPlugin.Proxy.AppEvtVirtualCamOff -= eventSwitchedOn;
            ObsStudioPlugin.Proxy.AppEvtVirtualCamOn -= eventSwitchedOff;
        }

        protected override void RunToggle() => ObsStudioPlugin.Proxy.AppToggleVirtualCam();
    }
}
