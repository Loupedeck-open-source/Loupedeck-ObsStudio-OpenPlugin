namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class StudioModeToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        public StudioModeToggleCommand()
                : base("Studio Mode toggle", "Enables or disables Studio Mode", /*no group*/"",
                new String[] {
                    "Command unavailable",
                    "Enable Studio Mode",
                    "Disable Studio Mode"
                },
                new String[] {
                  "Loupedeck.ObsPlugin.icons.SoftwareNotFound.png",
                  "Loupedeck.ObsPlugin.icons.STREAM_EnableStudioMode.png",
                  "Loupedeck.ObsPlugin.icons.STREAM_DisableStudioMode2.png"
                })
        {
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> onEvent, EventHandler<EventArgs> offEvent)
        {
            this.Proxy.AppEvtStudioModeOff += offEvent;
            this.Proxy.AppEvtStudioModeOn += onEvent;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> onEvent, EventHandler<EventArgs> offEvent)
        {
            this.Proxy.AppEvtStudioModeOff -= offEvent;
            this.Proxy.AppEvtStudioModeOn -= onEvent;
        }

        protected override void RunCommand(String actionParameter) => this.Proxy.AppToggleStudioMode();
    }
}
