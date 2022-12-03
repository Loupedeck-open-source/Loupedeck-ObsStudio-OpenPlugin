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

        private void OnObsRecordPaused(Object sender, EventArgs e) => this.AppEvtRecordingPaused?.Invoke(sender, e);

        private void OnObsRecordResumed(Object sender, EventArgs e) => this.AppEvtRecordingResumed?.Invoke(sender, e);

        // FIXME: Provide customized images for starting/started... -- For that, create special event handler on Action side.
        private void OnObsRecordingStateChange(OBSWebsocket sender, OBSWebsocketDotNet.Types.OutputState newState)
        {
            this.Plugin.Log.Info($"OBS Recording state change, new state is {newState}");

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

        public void AppToggleRecording()
        {
            if (this.IsAppConnected)
            {
                this.Plugin.Log.Info("Toggle recording");

                Helpers.TryExecuteSafe(() => this.ToggleRecording());
            }
        }

        public void AppStartRecording()
        {
            if (this.IsAppConnected)
            {
                this.Plugin.Log.Info("Start recording");

                Helpers.TryExecuteSafe(() => this.StartRecording());
            }
        }

        public void AppStopRecording()
        {
            if (this.IsAppConnected)
            {
                this.Plugin.Log.Info("Stop recording");

                Helpers.TryExecuteSafe(() => this.StopRecording());
            }
        }

        public void AppPauseRecording()
        {
            if (this.IsAppConnected)
            {
                this.Plugin.Log.Info("Pause recording");

                Helpers.TryExecuteSafe(() => this.PauseRecording());
            }
        }

        public void AppResumeRecording()
        {
            if (this.IsAppConnected)
            {
                this.Plugin.Log.Info("Resume recording");

                Helpers.TryExecuteSafe(() => this.ResumeRecording());
            }
        }
    }
}
