namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    class ReplayBufferToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;
        public ReplayBufferToggleCommand(): base(name: "ReplayBufferToggle",
                       displayName:   "Replay Buffer Toggle", 
                       description:   "Starts/Stops recording into the Replay Buffer", 
                       groupName:     "",
                       offStateName:  "Start recording into the Replay Buffer",
                       onStateName:   "Stop recording into the Replay Buffer",
                       offStateImage: "Loupedeck.ObsPlugin.icons.STREAM_StartReplayBuffer.png",
                       onStateImage:  "Loupedeck.ObsPlugin.icons.STREAM_StopReplayBuffer.png")
        {
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            this.Proxy.AppEvtReplayBufferOff += eventSwitchedOff;
            this.Proxy.AppEvtReplayBufferOn += eventSwitchedOn;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            this.Proxy.AppEvtReplayBufferOff -= eventSwitchedOff;
            this.Proxy.AppEvtReplayBufferOn -= eventSwitchedOn;
        }

        protected override void RunToggle() => this.Proxy.AppToggleReplayBuffer();
    }
}
