namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    class ReplayBufferToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        public ReplayBufferToggleCommand()
                : base("Replay Buffer Toggle", "Start/Stop recording into the Replay Buffer", /*no group*/"",
                new String[] {
                    "Command unavailable",
                    "Start recording into the Replay Buffer",
                    "Stop recording into the Replay Buffer."
                },
                new String[] {
                  "Loupedeck.ObsPlugin.icons.SoftwareNotFound.png",
                  "Loupedeck.ObsPlugin.icons.STREAM_StartReplayBuffer.png",
                  "Loupedeck.ObsPlugin.icons.STREAM_StopReplayBuffer.png"
                })
        {
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> onEvent, EventHandler<EventArgs> offEvent)
        {
            this.Proxy.AppEvtReplayBufferOff += offEvent;
            this.Proxy.AppEvtReplayBufferOn += onEvent;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> onEvent, EventHandler<EventArgs> offEvent)
        {
            this.Proxy.AppEvtReplayBufferOff -= offEvent;
            this.Proxy.AppEvtReplayBufferOn -= onEvent;
        }

        protected override void RunCommand(String actionParameter) => this.Proxy.AppToggleReplayBuffer();
    }
}
