namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        // STREAMING TOGGLE
        public event EventHandler<EventArgs> AppEvtStreamingOn;

        public event EventHandler<EventArgs> AppEvtStreamingOff;

        public void AppToggleStreaming()
        { 
            if (!this.StreamingStateChangeIsInProgress())
            {
                this.SafeRunConnected(() => this.ToggleStreaming(), "Cannot toggle streaming");
            }
        }

        public void AppStartStreaming() 
        { 
            if (!this.StreamingStateChangeIsInProgress())
            {
                this.SafeRunConnected(() => this.StartStreaming(), "Cannot start streaming");
            }
        }

        public void AppStopStreaming()
        {
            if (!this.StreamingStateChangeIsInProgress())
            {
                this.SafeRunConnected(() => this.StopStreaming(), "Cannot stop streaming");
            }
        }
        private OBSWebsocketDotNet.Types.OutputState _currentStreamingState = OBSWebsocketDotNet.Types.OutputState.Stopped;
        private Boolean StreamingStateChangeIsInProgress() => this._currentStreamingState == OBSWebsocketDotNet.Types.OutputState.Starting || this._currentStreamingState == OBSWebsocketDotNet.Types.OutputState.Stopping;

        private void OnObsStreamingStateChange(OBSWebsocket sender, OBSWebsocketDotNet.Types.OutputState newState)
        {
            this.Plugin.Log.Info($"OBS StreamingStateChange, new state {newState}");

            this._currentStreamingState = newState;

            if ((newState == OBSWebsocketDotNet.Types.OutputState.Started) || (newState == OBSWebsocketDotNet.Types.OutputState.Starting))
            {
                this.AppEvtStreamingOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtStreamingOff?.Invoke(this, new EventArgs());
            }
        }

    }
}
