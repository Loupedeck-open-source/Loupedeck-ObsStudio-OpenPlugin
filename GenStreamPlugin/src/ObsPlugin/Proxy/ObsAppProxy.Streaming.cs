namespace Loupedeck.ObsPlugin
{
    using System;
    using System.Collections.Generic;

    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class ObsAppProxy
    {
        // STREAMING TOGGLE
        public event EventHandler<EventArgs> AppEvtStreamingOn;

        public event EventHandler<EventArgs> AppEvtStreamingOff;

        private void OnObsStreamingStateChange(OBSWebsocket sender, OBSWebsocketDotNet.Types.OutputState newState)
        {
            ObsPlugin.Trace($"OBS StreamingStateChange, new state {newState}");

            if ((newState == OBSWebsocketDotNet.Types.OutputState.Started) || (newState == OBSWebsocketDotNet.Types.OutputState.Starting))
            {
                this.AppEvtStreamingOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtStreamingOff?.Invoke(this, new EventArgs());
            }
        }

        public void AppToggleStreaming()
        {
            if (this.IsAppConnected)
            {
                ObsPlugin.Trace("Toggling streaming");
                _ = Helpers.TryExecuteSafe(() => this.ToggleStreaming());
            }
        }
    }
}
