namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

    using OBSWebsocketDotNet;
    using OBSWebsocketDotNet.Types.Events;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        // Note.  Transition is also covered in this module
        public event EventHandler<EventArgs> AppEvtStudioModeOn;

        public event EventHandler<EventArgs> AppEvtStudioModeOff;

        public void AppRunTransition()
        {
            if (this.IsAppConnected && this._studioMode)
            {
                if (Helpers.TryExecuteSafe(() => this.TriggerStudioModeTransition()))
                {
                    this.Plugin.Log.Info("Transition executed successfully");
                }
                else
                {
                    this.Plugin.Log.Warning("Cannot run transition");
                }
            }
        }

        public void AppToggleStudioMode() => this.SafeRunConnected(() => this.SetStudioModeEnabled(!this.GetStudioModeEnabled()), "Cannot toggle studio mode");

        public void AppStartStudioMode() => this.SafeRunConnected(() => this.SetStudioModeEnabled(true), "Cannot start studio mode");

        public void AppStopStudioMode() => this.SafeRunConnected(() => this.SetStudioModeEnabled(false), "Cannot stop studio mode");
        // Caching studio mode
        private Boolean _studioMode = false;

        private void OnObsStudioModeStateChanged(Object _, StudioModeStateChangedEventArgs args)
            => this.OnObsStudioModeStateChanged(_, args.StudioModeEnabled);
        private void OnObsStudioModeStateChanged(Object _, Boolean Enabled)
        {
            this.Plugin.Log.Info($"OBS StudioMode State change, enabled={Enabled}");
            this._studioMode = Enabled;
            if(Enabled)
            {
                this.AppEvtStudioModeOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtStudioModeOff?.Invoke(this, new EventArgs());
            }
        }


    }
}
