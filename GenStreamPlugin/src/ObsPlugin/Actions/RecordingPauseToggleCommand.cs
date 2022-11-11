namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class RecordingPauseToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        public RecordingPauseToggleCommand()
                : base("Recording Pause", "Pauses/Resumes recording", /*no group*/"",
                new String[] {
                    "Command unavailable",
                    "Pause recording",
                    "Resume recording"
                },
                new String[] {
                  "Loupedeck.ObsPlugin.icons.SoftwareNotFound.png",
                  "Loupedeck.ObsPlugin.icons.STREAM_RecordPause.png",
                  "Loupedeck.ObsPlugin.icons.STREAM_RecordResume.png"
                })
        {
        }

        protected void OnAppRecordingStarted(Object sender, EventArgs e)
        {
            this.SetCurrentState(0); 
            this.IsEnabled = true;
        }

        protected void OnAppRecordingStopped(Object sender, EventArgs e)
        {
            this.IsEnabled = false;
            this.SetCurrentState(0); 
        }

        protected void OnAppRecordingResumed(Object sender, EventArgs e)
        {
            //Note this can be called from OnLoad as well (OffEvent) in which case we check if we are InRecording
            this.IsEnabled = this.Proxy.InRecording;
            this.ActionImageChanged();
        }

        protected void OnAppRecordingPaused(Object sender, EventArgs e)
        {
            this.ActionImageChanged();
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> onEvent, EventHandler<EventArgs> offEvent)
        {
            this.Proxy.AppEvtRecordingResumed += onEvent;
            this.Proxy.AppEvtRecordingPaused += offEvent;
            this.Proxy.AppEvtRecordingOff += this.OnAppRecordingStopped;
            this.Proxy.AppEvtRecordingOn += this.OnAppRecordingStarted;
            this.Proxy.AppEvtRecordingPaused += this.OnAppRecordingPaused;
            this.Proxy.AppEvtRecordingResumed += this.OnAppRecordingResumed;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> onEvent, EventHandler<EventArgs> offEvent)
        {
            this.Proxy.AppEvtRecordingResumed -= onEvent;
            this.Proxy.AppEvtRecordingPaused -= offEvent;
            this.Proxy.AppEvtRecordingOff -= this.OnAppRecordingStopped;
            this.Proxy.AppEvtRecordingOn -= this.OnAppRecordingStarted;
            this.Proxy.AppEvtRecordingPaused -= this.OnAppRecordingPaused;
            this.Proxy.AppEvtRecordingResumed -= this.OnAppRecordingResumed;

        }

        protected override void RunCommand(String actionParameter)
        {
            if(this.TryGetCurrentStateIndex(out var currentState))
            {
                if(currentState == 0)
                {
                    this.Proxy.AppPauseRecording();
                }
                else
                {
                    this.Proxy.AppResumeRecording();
                }
            }
        }
    }
}
