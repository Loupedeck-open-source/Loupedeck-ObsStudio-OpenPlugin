namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        public event EventHandler<MuteEventArgs> AppEvtSourceMuteStateChanged;
        public event EventHandler<VolumeEventArgs> AppEvtSourceVolumeChanged;

        public event EventHandler<SourceNameEventArgs> AppSourceCreated;
        public event EventHandler<SourceNameEventArgs> AppSourceDestroyed;

        private readonly Dictionary<String, String> _specialSources = new Dictionary<String, String>();
        private readonly List<String> _audioSourceTypes = new List<String>();

        internal Dictionary<String, AudioSourceDescriptor> CurrentAudioSources { get; private set; }  = new Dictionary<String, AudioSourceDescriptor>();

        private Boolean IsAudioSourceType(OBSWebsocketDotNet.Types.SourceSettings settings) => this._audioSourceTypes.Contains(settings.SourceKind ?? settings.SourceType);

        private void OnObsSourceAudioActivated(OBSWebsocket sender, String sourceName)
        {
            // NOTE: We do not testSettings (type of the source) -> It's audio for sure!
            if (this.AddCurrentAudioSource(sourceName, false, false))
            {
                this.AppSourceCreated?.InvokeMethod(sourceName);
            }
        }

        // NOTE: See if we need to do anything regarding
        private void OnObsSourceAudioDeactivated(OBSWebsocket sender, String sourceName) => this.OnObsSourceDestroyed(sender, sourceName, "", "");

        /// <summary>
        /// Adds a source to CurrentAudioSources list
        /// </summary>
        /// <param name="sourceName">Name of the source</param>
        /// <param name="testAudio">Test if source has audio active</param>
        /// <returns>True if source is added</returns>
        private Boolean AddCurrentAudioSource(String sourceName, Boolean testSettings = true, Boolean testAudio = true)
        {
            if (!this.CurrentAudioSources.ContainsKey(sourceName) &&
                    Helpers.TryExecuteFunc(
                        () => (!testSettings || this.IsAudioSourceType(this.GetSourceSettings(sourceName)))
                                                 && (!testAudio || this.GetAudioActive(sourceName)), out var good) && good)
            {
                this.CurrentAudioSources.Add(sourceName, new AudioSourceDescriptor(sourceName, this));
                this.Plugin.Log.Info($"Adding Regular audio source {sourceName}");
                return true;
            }
            return false;
        }

        private void OnObsSourceCreated(OBSWebsocket sender, OBSWebsocketDotNet.Types.SourceSettings settings)
        {
            // Check if we should care
            if (this.IsAudioSourceType(settings))
            {
                if (this.AddCurrentAudioSource(settings.SourceName, false, true))
                {
                    this.AppSourceCreated?.Invoke(this, new SourceNameEventArgs(settings.SourceName));
                }
            }
        }

        private void OnObsSourceDestroyed(OBSWebsocket sender, String sourceName, String sourceType, String sourceKind)
        {
            if (this.CurrentAudioSources.ContainsKey(sourceName))
            {
                this.CurrentAudioSources.Remove(sourceName);
                this.AppSourceDestroyed?.Invoke(this, new SourceNameEventArgs(sourceName));
            }
            else
            {
                this.Plugin.Log.Warning($"SourceDestroyed: Source {sourceName} is not found in audioSources");
            }
        }

        private void OnObsSourceVolumeChanged(OBSWebsocket sender, OBSWebsocketDotNet.Types.SourceVolume volDesc)
        {
            if (this.CurrentAudioSources.ContainsKey(volDesc.SourceName))
            {
                this.CurrentAudioSources[volDesc.SourceName].Volume = volDesc.Volume;
                this.AppEvtSourceVolumeChanged?.Invoke(sender, new VolumeEventArgs(volDesc.SourceName, volDesc.Volume, volDesc.VolumeDb));
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot update volume of {volDesc?.SourceName} --not present in current sources");
            }
        }

        private void OnObsSourceMuteStateChanged(OBSWebsocket sender, String sourceName, Boolean isMuted)
        {
            if (this.CurrentAudioSources.ContainsKey(sourceName))
            {
                this.CurrentAudioSources[sourceName].Muted = isMuted;
                this.AppEvtSourceMuteStateChanged?.Invoke(sender, new MuteEventArgs(sourceName, isMuted));
                this.Plugin.Log.Info($"OBS: OnObsSourceMuteStateChanged Source '{sourceName}' is muted '{isMuted}'");
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot update mute status. Source {sourceName} not in current sources");
            }
        }

        // NOTE: We are NOT going to OBS for mute and volume, using cached value instead -- This is for LD UI
        internal Boolean AppGetMute(String sourceName) =>
            this.IsAppConnected & this.CurrentAudioSources.ContainsKey(sourceName) && this.CurrentAudioSources[sourceName].Muted;

        internal Single AppGetVolume(String sourceName) =>
            this.IsAppConnected & this.CurrentAudioSources.ContainsKey(sourceName) ? this.CurrentAudioSources[sourceName].Volume : (Single)0.0;

        // Toggles mute on the source, returns current state of the mute.
        internal void AppToggleMute(String sourceName)
        {
            if (this.IsAppConnected && this.CurrentAudioSources.ContainsKey(sourceName))
            {
                try
                {
                    var mute = this.AppGetMute(sourceName);
                    this.SetMute(sourceName, !mute);
                    this.Plugin.Log.Info($"OBS: Setting mute to source '{sourceName}' to '{!mute}'");
                }
                catch (Exception ex)
                {
                    this.Plugin.Log.Error($"Exception {ex.InnerException.Message} -- Cannot set mute for source {sourceName}");
                }
            }
            else
            {
                this.Plugin.Log.Warning($"AppToggleMute: Source {sourceName} not found in current sources, ignoring");
            }
        }

        internal void AppSetVolume(String sourceName, Int32 diff_ticks)
        {
            if (this.IsAppConnected && this.CurrentAudioSources.ContainsKey(sourceName))
            {
                try
                {
                    var current = this.AppGetVolume(sourceName) + (diff_ticks / 100.0F);

                    current = (Single)(current < 0.0 ? 0.0 : (current > 1.0 ? 1.0 : current));

                    this.SetVolume(sourceName, current);
                }
                catch (Exception ex)
                {
                    this.Plugin.Log.Error($"Exception {ex.InnerException.Message} -- Cannot set volume for source {sourceName}");
                }
            }
            else
            {
                this.Plugin.Log.Warning($"AppSetVolume: Source {sourceName} not found in current sources, ignoring");
            }
        }

        // Executed once, upon connection. Note, throws!!
        private void OnAppConnected_RetreiveSourceTypes()
        {
            this._audioSourceTypes.Clear();

            foreach (var type in this.GetSourceTypesList())
            {
                if (type.Capabilities.HasAudio)
                {
                    this._audioSourceTypes.Add(type.TypeID);
                    this.Plugin.Log.Info($"Type {type.TypeID} will be handled as audio type");
                }
            }
        }

        // Retreive audio sources from current collection
        private void OnObsSceneCollectionChanged_RetreiveAudioSources()
        {
            this.CurrentAudioSources.Clear();
            try
            {
                foreach (var source in this.GetSourcesList())
                {
                    // NOTE: Special sources are seen as in GetSourcesList too! (they're present as 'value' specSource.Value)
                    if ( /*!this._specialSources.ContainsValue(source.Name)
                            &&*/ this.IsAudioSourceType(this.GetSourceSettings(source.Name))
                            && this.GetAudioActive(source.Name))
                    {
                        // Adding audio source and populating initial values
                        this.CurrentAudioSources.Add(source.Name, new AudioSourceDescriptor(source.Name, this));
                        this.Plugin.Log.Info($"Adding audio source {source.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                this.Plugin.Log.Error($"OnObsSceneCollectionChanged_RetreiveAudioSources: Exception '{ex.Message}' when retreiving list of sources from current scene collection!");
            }
        }
    }
}
