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
        public event EventHandler<EventArgs> AppEvtRecordingOn;
        public event EventHandler<EventArgs> AppEvtRecordingOff;
        public event EventHandler<EventArgs> AppEvtRecordingResumed;
        public event EventHandler<EventArgs> AppEvtRecordingPaused;
        public event EventHandler<IntParamArgs> AppEvtRecordingStateChange;

        public Boolean InRecording { get; private set; } = false;

        public Boolean IsRecordingPaused { get; private set; } = false;
        public void AppToggleRecording() => this.SafeRunConnected(() => this.ToggleRecording(), "Cannot toggle Recording");

        public void AppStartRecording() => this.SafeRunConnected(() => this.StartRecording(), "Cannot start Recording");

        public void AppStopRecording() => this.SafeRunConnected(() => this.StopRecording(), "Cannot stop Recording");
        public void AppToggleRecordingPause() => this.SafeRunConnected(() => this.ToggleRecordingPause(), "Cannot toggle Recorging Pause");

        public void AppPauseRecording() => this.SafeRunConnected(() => this.PauseRecording(), "Cannot pause recording");

        public void AppResumeRecording() => this.SafeRunConnected(() => this.ResumeRecording(), "Cannot resume recording");

        private void OnObsRecordPaused(Object sender, EventArgs e)
        {
            this.IsRecordingPaused = true;
            this.AppEvtRecordingPaused?.Invoke(sender, e);
        }

        private void OnObsRecordResumed(Object sender, EventArgs e)
        {
            this.IsRecordingPaused = false;
            this.AppEvtRecordingResumed?.Invoke(sender, e);
        }

        // FIXME: Provide customized images for starting/started... -- For that, create special event handler on Action side.
        private void OnObsRecordingStateChange(OBSWebsocket sender, OBSWebsocketDotNet.Types.OutputState newState)
        {
            this.Plugin.Log.Info($"OBS Recording state change, new state is {newState}");

            this.IsRecordingPaused = false;

            if ((newState == OBSWebsocketDotNet.Types.OutputState.Started) || (newState == OBSWebsocketDotNet.Types.OutputState.Starting))
            {
                this.InRecording = true;
                this.AppEvtRecordingOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.InRecording = false;
                this.AppEvtRecordingOff?.Invoke(this, new EventArgs());
            }

            this.AppEvtRecordingStateChange?.Invoke(this, new IntParamArgs((Int32)newState));
        }

        //A wrapper for Recording Pause Toggle, throws!
        private void ToggleRecordingPause()
        {
            if(this.IsRecordingPaused)
            {
                this.ResumeRecording();
            }
            else if(this.InRecording)
            {
                this.PauseRecording();
            }
        }
    }
}
