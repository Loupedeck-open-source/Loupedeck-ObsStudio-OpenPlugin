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
        public event EventHandler<EventArgs> AppEvtStudioModeOn;

        public event EventHandler<EventArgs> AppEvtStudioModeOff;

        private void OnObsStudioModeStateChange(Object sender, Boolean enabled)
        {
            this.Trace($"OBS StudioMode State change, enabled={enabled}");

            if (enabled)
            {
                this.AppEvtStudioModeOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtStudioModeOff?.Invoke(this, new EventArgs());
            }
        }

        public void AppToggleStudioMode()
        {
            if (this.IsAppConnected)
            {
                this.Trace("Toggling studio mode");
                Helpers.TryExecuteSafe(() => this.ToggleStudioMode());
            }
        }
    }
}
