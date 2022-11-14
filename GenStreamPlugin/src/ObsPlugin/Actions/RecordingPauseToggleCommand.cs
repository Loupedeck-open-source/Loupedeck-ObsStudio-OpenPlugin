namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class RecordingPauseToggleCommand : GenericOnOffSwitch
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        public RecordingPauseToggleCommand()
                : base(displayName:    "Recording Pause", 
                       description:    "Pauses/resumes recording", 
                       groupName:      "",
                       offStateName:   "Pause recording",
                       onStateName:    "Resume recording",
                       offStateImage:  "Loupedeck.ObsPlugin.icons.STREAM_RecordPause.png",
                       onStateImage:   "Loupedeck.ObsPlugin.icons.animated-pause.gif")
        {
        }

        protected void OnAppRecordingStarted(Object sender, EventArgs e)
        {
            //Note the command is only enabled if there is recording!
            this.TurnOff();
            this.IsEnabled = true;
            this.isPaused = false;
        }

        private Boolean isPaused = false; 

        protected void OnAppRecordingStopped(Object sender, EventArgs e)
        {
            this.TurnOff();
            this.IsEnabled = false;
            this.isPaused = false;
        }

        protected void OnAppRecordingResumed(Object sender, EventArgs e)
        {
            //Note this can be called from OnLoad as well (eventSwitchedOn) in which case we check if we are InRecording
            this.IsEnabled = this.Proxy.InRecording;
            this.isPaused = false;
            this.TurnOff();
            
        }

        protected void OnAppRecordingPaused(Object sender, EventArgs e)
        {
            this.isPaused = true;
            this.TurnOn();
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            this.Proxy.AppEvtRecordingResumed += eventSwitchedOff;
            this.Proxy.AppEvtRecordingPaused += eventSwitchedOn;

            this.Proxy.AppEvtRecordingOff += this.OnAppRecordingStopped;
            this.Proxy.AppEvtRecordingOn += this.OnAppRecordingStarted;
            this.Proxy.AppEvtRecordingPaused += this.OnAppRecordingPaused;
            this.Proxy.AppEvtRecordingResumed += this.OnAppRecordingResumed;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            this.Proxy.AppEvtRecordingResumed -= eventSwitchedOff;
            this.Proxy.AppEvtRecordingPaused -= eventSwitchedOn;
            this.Proxy.AppEvtRecordingOff -= this.OnAppRecordingStopped;
            this.Proxy.AppEvtRecordingOn -= this.OnAppRecordingStarted;
            this.Proxy.AppEvtRecordingPaused -= this.OnAppRecordingPaused;
            this.Proxy.AppEvtRecordingResumed -= this.OnAppRecordingResumed;

        }

        protected override void RunToggle()
        {
            if(this.isPaused)
            {
                this.Proxy.AppResumeRecording();
            }
            else
            {
                this.Proxy.AppPauseRecording();
            }
        }
    }
}
