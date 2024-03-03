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
        // STREAMING TOGGLE
        public event EventHandler<EventArgs> AppEvtStreamingOn;

        public event EventHandler<EventArgs> AppEvtStreamingOff;

        public void AppToggleStreaming()
        { 
            if (!this.StreamingStateChangeIsInProgress())
            {
                this.SafeRunConnected(() => this.ToggleStream(), "Cannot toggle streaming");
            }
        }

        public void AppStartStreaming() 
        { 
            if (!this.StreamingStateChangeIsInProgress())
            {
                this.SafeRunConnected(() => this.StartStream(), "Cannot start streaming");
            }
        }

        public void AppStopStreaming()
        {
            if (!this.StreamingStateChangeIsInProgress())
            {
                this.SafeRunConnected(() => this.StopStream(), "Cannot stop streaming");
            }
        }
        private OBSWebsocketDotNet.Types.OutputState _currentStreamingState = OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED;
        private Boolean StreamingStateChangeIsInProgress() => 
                this._currentStreamingState == OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STARTING 
             || this._currentStreamingState == OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING;

        private void OnObsStreamingStateChange(Object o, StreamStateChangedEventArgs args) 
            => this.OnObsStreamingStateChange(o, args.OutputState.State);

        private void OnObsStreamingStateChange(Object _, OBSWebsocketDotNet.Types.OutputState newState)
        {
            this.Plugin.Log.Info($"OBS StreamingStateChange, new state {newState}");

            this._currentStreamingState = newState;
            switch (newState)
            {
                case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STARTED:
                case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STARTING:
                    this.AppEvtStreamingOn?.Invoke(this, new EventArgs());
                    break;
                case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED:
                case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING:
                    this.AppEvtStreamingOff?.Invoke(this, new EventArgs());
                    break;
                case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_PAUSED:
                case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_RESUMED:
                    this.Plugin.Log.Info($"OBS StreamingStateChange, warning, unsolicited new state {newState}");
                    break;
            }
        }

    }
}
