namespace Loupedeck.ObsPlugin.Actions
{
    using System;
    using System.Collections.Generic;

    public class SourceVolumeAdjustment : PluginDynamicAdjustment
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        private const String IMGSourceSelected = "SourceOn.png";
        private const String IMGSourceUnselected = "SourceOff.png";
        private const String IMGSourceInaccessible = "SourceOff.png";
        private const String SourceNameUnknown = "Offline";
        // private const String SpecialSourceGroupName = "General Audio";

        public SourceVolumeAdjustment()
            : base(false)
        {
            this.Name = "DynamicSpecialSources";
            this.DisplayName = "Volume Mixer";
            this.Description = "Controls the volume of the audio sources in OBS Studio";
            this.GroupName = "Audio";
        }

        protected override Boolean OnLoad()
        {
            this.IsEnabled = false;
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
                ObsPlugin.Trace($"Warning: Cannot  parse actionParameter {actionParameter}");
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
            if (this.TryGetParameter(actionParameter, out _))
            {
                this._muteStates[actionParameter] = isMuted;

                this.ActionImageChanged(actionParameter);
            }
        }

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e) => this.ActionImageChanged();

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;// We expect to get SceneCollectionChange so doin' nothin' here.

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.IsEnabled = false;
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
            var sourceName = SourceNameUnknown;
            var imageName = IMGSourceInaccessible;
            var selected = false;
            if (SceneKey.TryParse(actionParameter, out var parsed))
            {
                sourceName = parsed.Source;

                selected = (parsed.Collection == this.Proxy.CurrentSceneCollection) && this._muteStates[actionParameter];

                imageName = parsed.Collection != this.Proxy.CurrentSceneCollection ? IMGSourceInaccessible : this._muteStates[actionParameter] ? IMGSourceUnselected : IMGSourceSelected;
            }

            return (this.Plugin as ObsPlugin).GetPluginCommandImage(imageSize, imageName, sourceName, selected);
             
        }

        private void OnSourceCreated(String sourceName)
        {
            this.AddSource(sourceName);
            this.ParametersChanged();
        }

        private void OnSourceDestroyed(String sourceName)
        {
            var key = SceneKey.Encode(this.Proxy.CurrentSceneCollection, sourceName);

            if (this.TryGetParameter(key, out _))
            {
                this.RemoveParameter(key);
                _ = this._muteStates.Remove(key);
                this.ParametersChanged();
            }
        }

        // Instead of State for Multistate actions, we will hold the mute state here
        private readonly Dictionary<String, Boolean> _muteStates = new Dictionary<String, Boolean>();

        internal void AddSource(String sourceName, Boolean isSpecialSource = false)
        {
            var key = SceneKey.Encode(this.Proxy.CurrentSceneCollection, sourceName);
            var displayName = sourceName +  (isSpecialSource ? "(G)" : "");
            this.AddParameter(key, displayName, this.GroupName);

            //Moving to same group this.AddParameter(key, $"{sourceName}", isSpecialSource ? SpecialSourceGroupName : this.GroupName);
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
