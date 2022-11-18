namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class ObsAppProxy
    {
        public SourceMuteStateChangedCallback AppEvtSourceMuteStateChanged;
        public SourceVolumeChangedCallback AppEvtSourceVolumeChanged;

        private readonly Dictionary<String, String> _specialSources = new Dictionary<String, String>();
        private readonly List<String> _audioSourceTypes = new List<String>();

        public Dictionary<String, AudioSourceDesc> CurrentAudioSources = new Dictionary<String, AudioSourceDesc>();

        public class AudioSourceDesc
        {
            public Boolean SpecialSource;
            public Boolean Muted;
            public Single Volume;

            public AudioSourceDesc(String name, OBSWebsocket that, Boolean isSpecSource = false)
            {
                this.Muted = false;
                this.Volume = 0;
                this.SpecialSource = isSpecSource;

                try
                {
                    var v = that.GetVolume(name);
                    this.Muted = v.Muted;
                    this.Volume = v.Volume;
                }
                catch (Exception ex)
                {
                    ObsStudioPlugin.Trace($"Exception {ex.Message} getting volume information for source {name}");
                }
            }
        }

        private Boolean IsAudioSourceType(OBSWebsocketDotNet.Types.SourceSettings settings) => this._audioSourceTypes.Contains(settings.SourceKind ?? settings.SourceType);

        public delegate void AppSourceCreatedCb(String sourceName);

        public AppSourceCreatedCb AppEvtSourceCreated;
        public AppSourceCreatedCb AppEvtSourceDestroyed;

        private void OnObsSourceAudioActivated(OBSWebsocket sender, String sourceName)
        {
            // NOTE: We do not testSettings (type of the source) -> It's audio for sure!
            if (this.AddCurrentAudioSource(sourceName, false, false))
            {
                this.AppEvtSourceCreated?.Invoke(sourceName);
            }
        }

        //NOTE: See if we need to do anything regarding 
        private void OnObsSourceAudioDeactivated(OBSWebsocket sender, String sourceName) => this.OnObsSourceDestroyed(sender, sourceName, "", "");

        /// <summary>
        /// Adds a source to CurrentAudioSources list
        /// </summary>
        /// <param name="sourceName">Name of the source</param>
        /// <param name="testAudio">Test if source has audio active</param>
        /// <returns>True if source is added</returns>
        internal Boolean AddCurrentAudioSource(String sourceName, Boolean testSettings=true, Boolean testAudio=true)
        {
            if(!this.CurrentAudioSources.ContainsKey(sourceName) &&
                    Helpers.TryExecuteFunc( () => (!testSettings || this.IsAudioSourceType(this.GetSourceSettings(sourceName)))
                                                  && (!testAudio || this.GetAudioActive(sourceName)), out var good) && good)
            {
                this.CurrentAudioSources.Add(sourceName, new AudioSourceDesc(sourceName, this));
                ObsStudioPlugin.Trace($"Adding Regular audio source {sourceName}");
                return true;
            }
            return false;
        }

        private void OnObsSourceCreated(OBSWebsocket sender, OBSWebsocketDotNet.Types.SourceSettings settings)
        {
            // Check if we should care
            if (this.IsAudioSourceType(settings) )
            {
                if( this.AddCurrentAudioSource(settings.SourceName, false, true) )
                {
                    this.AppEvtSourceCreated?.Invoke(settings.SourceName);
                }
                
            }
        }

        private void OnObsSourceDestroyed(OBSWebsocket sender, String sourceName, String sourceType, String sourceKind)
        {
            if (this.CurrentAudioSources.ContainsKey(sourceName))
            {
                _ = this.CurrentAudioSources.Remove(sourceName);
                this.AppEvtSourceDestroyed?.Invoke(sourceName);
            }
            else
            {
                ObsStudioPlugin.Trace($"SourceDestroyed: Source {sourceName} is not found in audioSources");
            }
        }

        private void OnObsSourceVolumeChanged(OBSWebsocket sender, OBSWebsocketDotNet.Types.SourceVolume volDesc)
        {
            if(this.CurrentAudioSources.ContainsKey(volDesc.SourceName))
            {
                this.CurrentAudioSources[volDesc.SourceName].Volume = volDesc.Volume;
                this.AppEvtSourceVolumeChanged?.Invoke(sender, volDesc);
            }
            else
            {
                ObsStudioPlugin.Trace($"OBS: Error updating volume: Source {volDesc?.SourceName} not in current sources");
            }
        }

        private void OnObsSourceMuteStateChanged(OBSWebsocket sender, String sourceName, Boolean isMuted)
        {
            if (this.CurrentAudioSources.ContainsKey(sourceName))
            {
                this.CurrentAudioSources[sourceName].Muted = isMuted;
                this.AppEvtSourceMuteStateChanged?.Invoke(sender, sourceName, isMuted);
            }
            else
            {
                ObsStudioPlugin.Trace($"OBS: Error updating mute: Source {sourceName} not in current sources");
            }
        }

        // NOTE: We are NOT going to OBS for mute and volume, using cached value instead -- This is for LD UI
        public Boolean AppGetMute(String sourceName) =>
            this.IsAppConnected & this.CurrentAudioSources.ContainsKey(sourceName) && this.CurrentAudioSources[sourceName].Muted;

        public Single AppGetVolume(String sourceName) =>
            this.IsAppConnected & this.CurrentAudioSources.ContainsKey(sourceName) ? this.CurrentAudioSources[sourceName].Volume : (Single)0.0;

        // Toggles mute on the source, returns current state of the mute.
        public void AppToggleMute(String sourceName)
        {
            if (this.IsAppConnected && this.CurrentAudioSources.ContainsKey(sourceName))
            {
                try
                {
                    var mute = this.AppGetMute(sourceName);
                    this.SetMute(sourceName, !mute);
                }
                catch (Exception ex)
                {
                    ObsStudioPlugin.Trace($"Warning: Exception {ex.InnerException.Message} -- Cannot set mute for source {sourceName}");
                }
            }
            else
            {
                ObsStudioPlugin.Trace($"Warning: source {sourceName} not found in current sources, ignoring");
            }
        }

        public void AppSetVolume(String sourceName, Int32 diff_ticks)
        {
            if (this.IsAppConnected && this.CurrentAudioSources.ContainsKey(sourceName))
            {
                try
                {
                    var current = this.AppGetVolume(sourceName) + (Single)diff_ticks / 100.0F;

                    current = (Single)(current < 0.0 ? 0.0 : (current > 1.0 ? 1.0 : current));

                    this.SetVolume(sourceName, current);
                }
                catch (Exception ex)
                {
                    ObsStudioPlugin.Trace($"Warning: Exception {ex.InnerException.Message} -- Cannot set volume for source {sourceName}");
                }
            }
            else
            {
                ObsStudioPlugin.Trace($"Warning: source {sourceName} not found in current sources, ignoring");
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
                    ObsStudioPlugin.Trace($"Type {type.TypeID} will be handled as audio type");
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
                    if (    /*!this._specialSources.ContainsValue(source.Name) 
                            &&*/ this.IsAudioSourceType(this.GetSourceSettings(source.Name))
                            && this.GetAudioActive(source.Name))
                    {
                        // Adding audio source and populating initial values
                        this.CurrentAudioSources.Add(source.Name, new AudioSourceDesc(source.Name, this));
                        ObsStudioPlugin.Trace($"Adding Regular audio source {source.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                ObsStudioPlugin.Trace($"Warning: Exception {ex.Message} when retreiving list of sources from current scene collection!");
            }
        }
    }
}
