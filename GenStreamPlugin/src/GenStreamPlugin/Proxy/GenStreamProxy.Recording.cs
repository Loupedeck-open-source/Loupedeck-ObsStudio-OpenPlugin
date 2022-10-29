namespace Loupedeck.GenStreamPlugin
{
    using System;
    using System.Collections.Generic;
    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class GenStreamProxy 
    {
        public event EventHandler<EventArgs> AppEvtRecordingOn;
        public event EventHandler<EventArgs> AppEvtRecordingOff;

        //FIXME: Provide customized images for starting/started... -- For that, create special event handler on Action side. 
        private void OnObsRecordingStateChange(OBSWebsocket sender, OBSWebsocketDotNet.Types.OutputState newState)
        {
            if ((newState == OBSWebsocketDotNet.Types.OutputState.Started) || (newState == OBSWebsocketDotNet.Types.OutputState.Starting))
            {
                this.AppEvtRecordingOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtRecordingOff?.Invoke(this, new EventArgs());
            }
        }
        public void AppToggleRecording()
        {
            if (this.IsAppConnected)
            {
                Helpers.TryExecuteSafe(() => this.ToggleRecording());
            }
        }

    }
}
