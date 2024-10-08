﻿namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;
    using System.Collections.Generic;

    internal class SourceVolumeAdjustment : PluginDynamicAdjustment
    {
        private const String IMGSourceSelected = "AudioMixerUnmuted.svg";
        private const String IMGSourceUnselected = "AudioMixerMuted.svg";
        private const String IMGSourceInaccessible = "AudioDisabled.png";
        private const String SourceNameUnknown = "N/A";

        // private const String SpecialSourceGroupName = "General Audio";

        public SourceVolumeAdjustment()
            : base(false)
        {
            this.DisplayName = "Volume Mixer";
            this.Description = "Controls the volume of the audio sources in OBS Studio";
            this.GroupName = "3. Audio";
        }

        protected override Boolean OnLoad()
        {
            this.IsEnabled = false;
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            ObsStudioPlugin.Proxy.AppEvtSourceMuteStateChanged += this.OnSourceMuteStateChanged;
            ObsStudioPlugin.Proxy.AppEvtSourceVolumeChanged += this.OnSourceVolumeChanged;

            ObsStudioPlugin.Proxy.AppSourceCreated += this.OnSourceCreated;
            ObsStudioPlugin.Proxy.AppSourceDestroyed += this.OnSourceDestroyed;

            ObsStudioPlugin.Proxy.AppInputRenamed += this.OnSourceRenamed;

            this.OnAppDisconnected(this, null);

            return true;
        }

        //FIXME:  When flipping between scene collections,  the icons for adjustments are updated with delay
        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;

            ObsStudioPlugin.Proxy.AppEvtSourceMuteStateChanged -= this.OnSourceMuteStateChanged;
            ObsStudioPlugin.Proxy.AppEvtSourceVolumeChanged -= this.OnSourceVolumeChanged;

            ObsStudioPlugin.Proxy.AppSourceCreated -= this.OnSourceCreated;
            ObsStudioPlugin.Proxy.AppSourceDestroyed -= this.OnSourceDestroyed;

            ObsStudioPlugin.Proxy.AppInputRenamed -= this.OnSourceRenamed;

            return true;
        }

        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            //TODO: Global audio sources do not need to have scene name in the action parameter. We can come up with the better way to handle this.
            return (SceneItemKey.TryParse(actionParameter, out var key) 
                && key.Collection.Equals(ObsStudioPlugin.Proxy.CurrentSceneCollection) 
                && ObsStudioPlugin.Proxy.AllSceneItems.ContainsKey(actionParameter))
            ? ObsStudioPlugin.Proxy.AllSceneItems[actionParameter].SourceName
            : SourceNameUnknown;
        }
        

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            if (SceneKey.TryParse(actionParameter, out var key) && key.Collection.Equals(ObsStudioPlugin.Proxy.CurrentSceneCollection))
            {
                ObsStudioPlugin.Proxy.AppSetVolume(key.Source, diff);
                this.AdjustmentValueChanged();
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (SceneKey.TryParse(actionParameter, out var key) && key.Collection.Equals(ObsStudioPlugin.Proxy.CurrentSceneCollection))
            {
                // Pressing the button toggles mute
                ObsStudioPlugin.Proxy.AppToggleMute(key.Source);
            }
            else
            {
                this.Plugin.Log.Warning($"Warning: Cannot  parse actionParameter {actionParameter}");
            }
        }

        protected override String GetAdjustmentValue(String actionParameter) 
        {
            return SceneKey.TryParse(actionParameter, out var key) && key.Collection.Equals(ObsStudioPlugin.Proxy.CurrentSceneCollection)
                    ? ObsStudioPlugin.Proxy.AppGetVolumeLabel(key.Source)
                    : ObsAppProxy.NotFoundVolumeLabel; /*so no n/a is displayed*/
        }
        private void OnSourceMuteStateChanged(Object sender, MuteEventArgs args)
        {
            var actionParameter = SceneKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, args.SourceName);

            // FIXME: Check if this 'has parameter' check is needed.
            if (this.TryGetParameter(actionParameter, out _) && this._muteStates.ContainsKey(actionParameter))
            {
                this._muteStates[actionParameter] = args.isMuted;

                this.ActionImageChanged(actionParameter);
            }
        }

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);
        

        private void OnCurrentSceneChanged(Object sender, EventArgs e) => this.ActionImageChanged();

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true; // We expect to get SceneCollectionChange so doin' nothin' here.

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.IsEnabled = false;
            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        private void OnSourceVolumeChanged(Object sender, VolumeEventArgs args)
        {
            var actionParameter = SceneKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, args.SourceName);
            this.AdjustmentValueChanged(actionParameter);
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var imageName = IMGSourceInaccessible;
            var selected = false;
            if (SceneKey.TryParse(actionParameter, out var parsed))
            {
                if (this._muteStates.ContainsKey(actionParameter)
                    && parsed.Collection == ObsStudioPlugin.Proxy.CurrentSceneCollection)
                {
                    selected = this._muteStates[actionParameter];
                    imageName = this._muteStates[actionParameter]
                                ? IMGSourceUnselected
                                : IMGSourceSelected;
                }
            }

            return EmbeddedResources.ReadBinaryFile(ObsStudioPlugin.ImageResPrefix + imageName).ToImage();
        }

        private void OnSourceCreated(Object sender, SourceNameEventArgs args)
        {
            this.AddSource(args.SourceName);
            this.ParametersChanged();
        }

        private void OnSourceDestroyed(Object sender, SourceNameEventArgs args)
        {
            var key = SceneKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, args.SourceName);

            if (this.TryGetParameter(key, out _))
            {
                this.RemoveParameter(key);
                _ = this._muteStates.Remove(key);
                this.ParametersChanged();
            }
        }

        private void OnSourceRenamed(Object sender, OldNewStringChangeEventArgs args)
        {
            var key = SceneKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, args.Old);

            if (this.TryGetParameter(key, out _))
            {
                this.RemoveParameter(key);
                _ = this._muteStates.Remove(key);
                this.AddSource(args.New);
                this.ParametersChanged();
            }
        }

        // Instead of State for Multistate actions, we will hold the mute state here
        private readonly Dictionary<String, Boolean> _muteStates = new Dictionary<String, Boolean>();

        private void AddSource(String sourceName, Boolean isSpecialSource = false)
        {
            var key = SceneKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, sourceName);
            var displayName = sourceName + (isSpecialSource ? "(G)" : "");
            this.AddParameter(key, displayName, this.GroupName).Description = $"Control volume of audio source \"{sourceName}\"";

            // Moving to same group this.AddParameter(key, $"{sourceName}", isSpecialSource ? SpecialSourceGroupName : this.GroupName);
            this._muteStates[key] = ObsStudioPlugin.Proxy.AppGetMute(sourceName);
        }

        private void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();
            this._muteStates.Clear();
            if (readContent)
            {
                foreach (var item in ObsStudioPlugin.Proxy.CurrentAudioSources)
                {
                    this.AddSource(item.Key, item.Value.SpecialSource);
                }
            }

            this.ParametersChanged();
            this.ActionImageChanged();
        }
    }
}
