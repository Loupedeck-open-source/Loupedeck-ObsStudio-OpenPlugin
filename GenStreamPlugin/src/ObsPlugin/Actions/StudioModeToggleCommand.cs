namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class StudioModeToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        public StudioModeToggleCommand()
                  : base(name: "StudioMode",
                    displayName: "Studio Mode Toggle",
                    description:  "Enables or disables Studio Mode",
                    groupName:    "",
                    offStateName: "Enable Studio Mode",
                    onStateName:  "Disable Studio Mode",
                    offStateImage:"STREAM_EnableStudioMode.png",
                    onStateImage: "animated-studio.gif")
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
