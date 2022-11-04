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

        public class IntParamArgs : EventArgs
        {
            public IntParamArgs(Int32 v) => this.State = v;

            public Int32 State { get; set; }
        }

        public event EventHandler<IntParamArgs> AppEvtRecordingStateChange;

        // FIXME: Provide customized images for starting/started... -- For that, create special event handler on Action side.
        private void OnObsRecordingStateChange(OBSWebsocket sender, OBSWebsocketDotNet.Types.OutputState newState)
        {
            this.Trace($"OBS Recording state change, new state is {newState}");
            
            if ((newState == OBSWebsocketDotNet.Types.OutputState.Started) || (newState == OBSWebsocketDotNet.Types.OutputState.Starting))
            {
                this.AppEvtRecordingOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtRecordingOff?.Invoke(this, new EventArgs());
            }

            this.AppEvtRecordingStateChange?.Invoke(this, new IntParamArgs((Int32)newState));
        }
        public void AppToggleRecording()
        {
            if (this.IsAppConnected)
            {
                this.Trace("Toggle recording");

                Helpers.TryExecuteSafe(() => this.ToggleRecording());
            }
        }

        public void AppStartRecording()
        {
            if (this.IsAppConnected)
            {
                this.Trace("Start recording");

                Helpers.TryExecuteSafe(() => this.StartRecording());
            }
        }

        public void AppStopRecording()
        {
            if (this.IsAppConnected)
            {
                this.Trace("Stop recording");

                Helpers.TryExecuteSafe(() => this.StopRecording());
            }
        }

    }
}
