namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;
    using System.Collections.Generic;

    public class SourceVolumeAdjustment : PluginDynamicAdjustment
    {
        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;

        private const String IMGSourceSelected = "Loupedeck.GenStreamPlugin.icons.SourceOn.png";
        private const String IMGSourceUnselected = "Loupedeck.GenStreamPlugin.icons.SourceOff.png";
        private const String IMGSourceInaccessible = "Loupedeck.GenStreamPlugin.icons.CloseDesktop.png";
        private const String IMGOffline = "Loupedeck.GenStreamPlugin.icons.SoftwareNotFound.png";
        private const String SourceNameUnknown = "Offline";
        private const String SpecialSourceGroupName = "General Audio";

        public SourceVolumeAdjustment()
            : base(false)
        {
            this.Name = "Audio Source Volume Mixer";
            this.DisplayName = "Volume Mixer";
            this.Description = "Controls Audio Source Volume";
            this.GroupName = "Audio Sources";
        }

        protected override Boolean OnLoad()
        {
            this.Proxy.EvtAppConnected += this.OnAppConnected;
            this.Proxy.EvtAppDisconnected += this.OnAppDisconnected;

            this.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            this.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            this.Proxy.AppEvtSourceMuteStateChanged += this.OnSourceMuteStateChanged;
            this.Proxy.AppEvtSourceVolumeChanged += this.OnSourceVolumeChanged;

            this.Proxy.AppEvtSourceCreated += this.OnSourceCreated;
            this.Proxy.AppEvtSourceDestroyed += this.OnSourceDestroyed;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.EvtAppConnected -= this.OnAppConnected;
            this.Proxy.EvtAppDisconnected -= this.OnAppDisconnected;

            this.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            this.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;

            this.Proxy.AppEvtSourceMuteStateChanged -= this.OnSourceMuteStateChanged;
            this.Proxy.AppEvtSourceVolumeChanged -= this.OnSourceVolumeChanged;

            this.Proxy.AppEvtSourceCreated -= this.OnSourceCreated;
            this.Proxy.AppEvtSourceDestroyed -= this.OnSourceDestroyed;

            return true;
        }

        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize) => SceneItemKey.TryParse(actionParameter, out var key) ? key.Source : SourceNameUnknown;

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            if (SceneKey.TryParse(actionParameter, out var key))
            {
                this.Proxy.AppSetVolume(key.Source, diff);
            }

            this.AdjustmentValueChanged();
        }

        protected override void RunCommand(String actionParameter)
        {
            if (SceneKey.TryParse(actionParameter, out var key))
            {
                // Pressing the button toggles mute
                this.Proxy.AppToggleMute(key.Source);
            }
            else
            {
                this.Proxy.Trace($"Warning: Cannot  parse actionParameter {actionParameter}");
            }
        }

        protected override String GetAdjustmentValue(String actionParameter)
        {
            var volume = "N/A";
            if (SceneKey.TryParse(actionParameter, out var key))
            {
                var number = this.Proxy.AppGetVolume(key.Source) * 100.0F;
                volume = number.ToString("0.");
            }

            return volume;
        }

        protected void OnSourceMuteStateChanged(OBSWebsocketDotNet.OBSWebsocket sender, String sourceName, Boolean isMuted)
        {
            var actionParameter = SceneKey.Encode(this.Proxy.CurrentSceneCollection, sourceName);

            // FIXME: Check if this 'has parameter' check is needed.
            if (this.TryGetParameter(actionParameter, out var param))
            {
                this._muteStates[actionParameter] = isMuted;

                this.ActionImageChanged();
            }
        }

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e) => this.ActionImageChanged();

        private void OnAppConnected(Object sender, EventArgs e)
        {
            // We expect to get SceneCollectionChange so doin' nothin' here.
        }

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected void OnSourceVolumeChanged(OBSWebsocketDotNet.OBSWebsocket sender, OBSWebsocketDotNet.Types.SourceVolume volume)
        {
            var actionParameter = SceneKey.Encode(this.Proxy?.CurrentSceneCollection, volume.SourceName);
            this.AdjustmentValueChanged(actionParameter);
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            // FIXME: Need proper images etc

            var sourceName = SourceNameUnknown;
            var imageName = IMGOffline;
            if (SceneKey.TryParse(actionParameter, out var parsed))
            {
                sourceName = parsed.Source;

                imageName = parsed.Collection != this.Proxy.CurrentSceneCollection ? IMGSourceInaccessible : this._muteStates[actionParameter] ? IMGSourceUnselected : IMGSourceSelected;
            }

            return GenStreamPlugin.NameOverBitmap(imageSize, imageName, sourceName);
        }

        private void OnSourceCreated(String sourceName)
        {
            this.AddSource(sourceName);
            this.ParametersChanged();
        }

        private void OnSourceDestroyed(String sourceName)
        {
            var key = SceneKey.Encode(this.Proxy.CurrentSceneCollection, sourceName);
            if (this.TryGetParameter(key, out var param))
            {
                this.RemoveParameter(key);
                this._muteStates.Remove(key);
                this.ParametersChanged();
            }
        }

        // Instead of State for Multistate actions, we will hold the mute state here
        private readonly Dictionary<String, Boolean> _muteStates = new Dictionary<String, Boolean>();

        internal void AddSource(String sourceName, Boolean isSpecialSource = false)
        {
            var key = SceneKey.Encode(this.Proxy.CurrentSceneCollection, sourceName);
            this.AddParameter(key, $"{sourceName}", isSpecialSource ? SpecialSourceGroupName : this.GroupName);
            this._muteStates[key] = this.Proxy.AppGetMute(sourceName);
        }

        internal void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();
            this._muteStates.Clear();
            if (readContent)
            {
                foreach (var item in this.Proxy.CurrentAudioSources)
                {
                    this.AddSource(item.Key, item.Value.SpecialSource);
                }
            }

            this.ParametersChanged();
        }
    }
}
