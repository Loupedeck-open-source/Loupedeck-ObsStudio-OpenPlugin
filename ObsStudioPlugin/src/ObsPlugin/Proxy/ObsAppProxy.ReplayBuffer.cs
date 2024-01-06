namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

    using OBSWebsocketDotNet;
    using OBSWebsocketDotNet.Types.Events;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        public event EventHandler<EventArgs> AppEvtReplayBufferOn;

        public event EventHandler<EventArgs> AppEvtReplayBufferOff;

        
        public void AppToggleReplayBuffer() => this.SafeRunConnected(() => this.ToggleReplayBuffer(), "Cannot toggle Replay Buffer");

        public void AppStartReplayBuffer() => this.SafeRunConnected(() => this.StartReplayBuffer(), "Cannot start Replay Buffer");

        public void AppStopReplayBuffer() => this.SafeRunConnected(() => this.StopReplayBuffer(), "Cannot stop Replay Buffer");

        public void AppSaveReplayBuffer()
        {
            if (this.IsAppConnected)
            {
                if (!Helpers.TryExecuteSafe(() => this.SaveReplayBuffer()))
                {
                    this.Plugin.Log.Warning("Cannot save replayBuffer");
                }
            }
        }
        private void OnObsReplayBufferStateChange(Object _, ReplayBufferStateChangedEventArgs args)
        {
            
            this.Plugin.Log.Info($"OBS Replay buffer state change, new state active {args.OutputState.IsActive}");
            
            if (args.OutputState.IsActive)
            {
                this.AppEvtReplayBufferOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtReplayBufferOff?.Invoke(this, new EventArgs());
            }
        }

    }
}
