namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class RecordingPauseToggleCommand : GenericOnOffSwitch
    {
        
        public RecordingPauseToggleCommand()
                : base(
                    name: "RecordingPause",
                    displayName: "Recording Pause",
                    description: "Pauses/resumes recording",
                    groupName: "",
                    offStateName: "Pause recording",
                    onStateName: "Resume recording",
                    offStateImage: "STREAM_RecordPause.png",
                    onStateImage: "STREAM_RecordResume.png")
        {
        }

        private void OnAppRecordingStarted(Object sender, EventArgs e)
        {
            // Note the command is only enabled if there is recording!
            this.TurnOff();
            this.IsEnabled = true;
        }

        private void OnAppRecordingStopped(Object sender, EventArgs e)
        {
            this.TurnOff();
            this.IsEnabled = false;
        }

        private void OnAppRecordingResumed(Object sender, EventArgs e)
        {
            // Note this can be called from OnLoad as well (eventSwitchedOn) in which case we check if we are InRecording
            this.IsEnabled = ObsStudioPlugin.Proxy.InRecording;
            this.TurnOff();
        }

        private void OnAppRecordingPaused(Object sender, EventArgs e) => this.TurnOn();

        protected override void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            ObsStudioPlugin.Proxy.AppEvtRecordingResumed += eventSwitchedOff;
            ObsStudioPlugin.Proxy.AppEvtRecordingPaused += eventSwitchedOn;

            ObsStudioPlugin.Proxy.AppEvtRecordingOff += this.OnAppRecordingStopped;
            ObsStudioPlugin.Proxy.AppEvtRecordingOn += this.OnAppRecordingStarted;
            ObsStudioPlugin.Proxy.AppEvtRecordingPaused += this.OnAppRecordingPaused;
            ObsStudioPlugin.Proxy.AppEvtRecordingResumed += this.OnAppRecordingResumed;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            ObsStudioPlugin.Proxy.AppEvtRecordingResumed -= eventSwitchedOff;
            ObsStudioPlugin.Proxy.AppEvtRecordingPaused -= eventSwitchedOn;
            ObsStudioPlugin.Proxy.AppEvtRecordingOff -= this.OnAppRecordingStopped;
            ObsStudioPlugin.Proxy.AppEvtRecordingOn -= this.OnAppRecordingStarted;
            ObsStudioPlugin.Proxy.AppEvtRecordingPaused -= this.OnAppRecordingPaused;
            ObsStudioPlugin.Proxy.AppEvtRecordingResumed -= this.OnAppRecordingResumed;
        }
        protected override void RunCommand(TwoStateCommand command)
        {
            switch (command)
            {
                case TwoStateCommand.TurnOff:
                    ObsStudioPlugin.Proxy.AppResumeRecording();
                    break;

                case TwoStateCommand.TurnOn:
                    ObsStudioPlugin.Proxy.AppPauseRecording();
                    break;

                case TwoStateCommand.Toggle:
                    ObsStudioPlugin.Proxy.AppToggleRecordingPause();
                    break;
            }
        }
    }
}
