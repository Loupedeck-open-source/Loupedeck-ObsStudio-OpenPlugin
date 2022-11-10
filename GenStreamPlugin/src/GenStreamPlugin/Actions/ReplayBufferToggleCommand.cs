namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;

    class ReplayBufferToggleCommand : GenericOnOffSwitch
    {
        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;

        public ReplayBufferToggleCommand()
                : base("Replay Buffer", "Toggles Replay Buffer", /*no group*/"",
                new String[] {
                    "Command unavailable",
                    "Start Replay Buffer",
                    "Stop Replay Buffer"
                },
                new String[] {
                  "Loupedeck.GenStreamPlugin.icons.SoftwareNotFound.png",
                  "Loupedeck.GenStreamPlugin.icons.STREAM_StartReplayBuffer.png",
                  "Loupedeck.GenStreamPlugin.icons.STREAM_StopReplayBuffer.png"
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
