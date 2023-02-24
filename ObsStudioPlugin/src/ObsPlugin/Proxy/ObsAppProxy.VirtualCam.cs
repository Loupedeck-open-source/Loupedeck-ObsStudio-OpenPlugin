namespace Loupedeck.ObsStudioPlugin
{
    using System;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        public event EventHandler<EventArgs> AppEvtVirtualCamOn;

        public event EventHandler<EventArgs> AppEvtVirtualCamOff;

        public void AppToggleVirtualCam() => this.SafeRunConnected(() => this.ToggleVirtualCam(), "Cannot toggle virtual cam");

        public void AppStartVirtualCam() => this.SafeRunConnected(() => this.StartVirtualCam(), "Cannot start virtual cam");

        public void AppStopVirtualCam() => this.SafeRunConnected(() => this.StopVirtualCam(), "Cannot stop virtual cam");

        private void OnObsVirtualCameraStarted(Object sender, EventArgs e)
        {
            this.Plugin.Log.Info("Obs Virtual camera started");
            this.AppEvtVirtualCamOn?.Invoke(this, new EventArgs());
        }

        private void OnObsVirtualCameraStopped(Object sender, EventArgs e)
        {
            this.Plugin.Log.Info("Obs Virtual camera stopped");
            this.AppEvtVirtualCamOff?.Invoke(this, new EventArgs());
        }

    }
}
