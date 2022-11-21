namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    public class StudioModeToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;

        public StudioModeToggleCommand()
                  : base(
                      name: "StudioMode",
                      displayName: "Studio Mode Toggle",
                      description: "Switches the OBS Studio Mode on/off, allowing you to change and edit Scenes in the background",
                      groupName: "",
                      offStateName: "Enable Studio Mode",
                      onStateName: "Disable Studio Mode",
                      offStateImage: "STREAM_EnableStudioMode.png",
                      onStateImage: "STREAM_DisableStudioMode2.png")
        {
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            this.Proxy.AppEvtStudioModeOff += eventSwitchedOn;
            this.Proxy.AppEvtStudioModeOn += eventSwitchedOff;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            this.Proxy.AppEvtStudioModeOff -= eventSwitchedOn;
            this.Proxy.AppEvtStudioModeOn -= eventSwitchedOff;
        }

        protected override void RunToggle() => this.Proxy.AppToggleStudioMode();
    }
}
