namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Remoting.Messaging;

    using OBSWebsocketDotNet;
    using OBSWebsocketDotNet.Types.Events;

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
        public void AppToggleRecording() => this.SafeRunConnected(() => this.ToggleRecord(), "Cannot toggle Recording");

        public void AppStartRecording() => this.SafeRunConnected(() => this.StartRecord(), "Cannot start Recording");

        public void AppStopRecording() => this.SafeRunConnected(() => this.StopRecord(), "Cannot stop Recording");
        public void AppToggleRecordingPause() => this.SafeRunConnected(() => this.ToggleRecordingPause(), "Cannot toggle Recorging Pause");

        public void AppPauseRecording() => this.SafeRunConnected(() => this.PauseRecord(), "Cannot pause recording");

        public void AppResumeRecording() => this.SafeRunConnected(() => this.ResumeRecord(), "Cannot resume recording");

        // FIXME: Provide customized images for starting/started... -- For that, create special event handler on Action side.
        private void OnObsRecordingStateChange(Object o, RecordStateChangedEventArgs args) 
            => this.OnObsRecordingStateChange(o, args.OutputState.State);

        private void OnObsRecordingStateChange(Object _, OBSWebsocketDotNet.Types.OutputState newState)
        {
            
            this.Plugin.Log.Info($"OBS Recording state change, new state is {newState}");

            this.IsRecordingPaused = false;
            switch (newState)
            {
                case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STARTED:
                case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STARTING:
                    this.InRecording = true;
                    this.AppEvtRecordingOn?.Invoke(this, new EventArgs());
                    break;
                case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED:
                case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING:
                    this.InRecording = false;
                    this.AppEvtRecordingOff?.Invoke(this, new EventArgs());
                    break;
                case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_PAUSED:
                    this.IsRecordingPaused = true;
                    this.AppEvtRecordingPaused?.Invoke(this, new EventArgs());
                    break;
                case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_RESUMED:
                    this.IsRecordingPaused = false;
                    this.AppEvtRecordingResumed?.Invoke(this, new EventArgs());
                    break;
            }

            this.AppEvtRecordingStateChange?.Invoke(this, new IntParamArgs((Int32)newState));
        }

        //A wrapper for Recording Pause Toggle, throws!
        private void ToggleRecordingPause()
        {
            if(this.IsRecordingPaused)
            {
                this.ResumeRecord();
            }
            else if(this.InRecording)
            {
                this.PauseRecord();
            }
        }
    }
}
