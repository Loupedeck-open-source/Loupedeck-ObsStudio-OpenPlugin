﻿namespace Loupedeck.ObsPlugin
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
                    Tracer.Trace($"Exception {ex.Message} getting volume information for source {name}");
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
                this.Trace($"Adding Regular audio source {sourceName}");
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
                this.CurrentAudioSources.Remove(sourceName);
                this.AppEvtSourceDestroyed?.Invoke(sourceName);
            }
            else
            {
                this.Trace($"SourceDestroyed: Source {sourceName} is not found in audioSources");
            }
        }

        private void OnObsSourceVolumeChanged(OBSWebsocket sender, OBSWebsocketDotNet.Types.SourceVolume volDesc)
        {
            if (!Helpers.TryExecuteAction(() => this.CurrentAudioSources[volDesc.SourceName].Volume = volDesc.Volume))
            {
                this.Trace($"OBS: Error updating volume to {volDesc?.Volume} for source {volDesc?.SourceName}");
            }

            this.AppEvtSourceVolumeChanged?.Invoke(sender, volDesc);
        }

        private void OnObsSourceMuteStateChanged(OBSWebsocket sender, String sourceName, Boolean isMuted)
        {
            if (!Helpers.TryExecuteAction(() => this.CurrentAudioSources[sourceName].Muted = isMuted))
            {
                this.Trace($"OBS: Error setting muted state to {isMuted} for source {sourceName}");
            }

            this.AppEvtSourceMuteStateChanged?.Invoke(sender, sourceName, isMuted);
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
                    this.Trace($"Warning: Exception {ex.InnerException.Message} -- Cannot set mute for source {sourceName}");
                }
            }
            else
            {
                this.Trace($"Warning: source {sourceName} not found in current sources, ignoring");
            }
        }

        public void AppSetVolume(String sourceName, Int32 diff_ticks)
        {
            if (this.IsAppConnected && this.CurrentAudioSources.ContainsKey(sourceName))
            {
                try
                {
                    var current = this.AppGetVolume(sourceName) + ((Single)diff_ticks / 100.0F);

                    current = (Single)(current < 0.0 ? 0.0 : (current > 1.0 ? 1.0 : current));

                    this.SetVolume(sourceName, current);
                }
                catch (Exception ex)
                {
                    this.Trace($"Warning: Exception {ex.InnerException.Message} -- Cannot set volume for source {sourceName}");
                }
            }
            else
            {
                this.Trace($"Warning: source {sourceName} not found in current sources, ignoring");
            }
        }

        // Executed once, upon connection
        private void OnAppConnected_RetreiveSourceTypes()
        {
            this._audioSourceTypes.Clear();

            if (!Helpers.TryExecuteAction(() =>
                {
                    foreach (var type in this.GetSourceTypesList())
                    {
                        if (type.Capabilities.HasAudio)
                        {
                            this._audioSourceTypes.Add(type.TypeID);
                            this.Trace($"Type {type.TypeID} will be handled as audio type");
                        }
                    }
                }))
            {
                this.Trace($"Warning: Cannot get list of supported audio types from OBS");
            }
        }

        private void OnAppConnected_RetreiveSpecialSources()
        {
            this._specialSources.Clear();

            if (Helpers.TryExecuteFunc(() => this.GetSpecialSources(), out var sources))
            {
                foreach (var source in sources)
                {
                    this._specialSources.Add(source.Key, source.Value);
                    this.Trace($"Adding Special source {source.Key} Val {source.Value}");
                }
            }
            else
            {
                this.Trace($"Warning: Cannot retreive list of special sources");
            }
        }

        // Retreive audio sources from current collection
        private void OnObsSceneCollectionChanged_RetreiveAudioSources()
        {
            this.CurrentAudioSources.Clear();
            try
            {
                /*foreach (var specSource in this._specialSources)
                {
                    this.CurrentAudioSources.Add(specSource.Key, new AudioSourceDesc(specSource.Key, this, true));
                    this.Trace($"Adding General audio source {specSource.Key}");
                }
                */
                foreach (var source in this.GetSourcesList())
                {
                    // NOTE: Special sources are seen as in GetSourcesList too! (they're present as 'value' specSource.Value)
                    if (    /*!this._specialSources.ContainsValue(source.Name) 
                            &&*/ this.IsAudioSourceType(this.GetSourceSettings(source.Name))
                            && this.GetAudioActive(source.Name))
                    {
                        // Adding audio source and populating initial values
                        this.CurrentAudioSources.Add(source.Name, new AudioSourceDesc(source.Name, this));
                        this.Trace($"Adding Regular audio source {source.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                // FIXME: Add Plugin Status -> Error here and to similar places
                this.Trace($"Warning: Exception {ex.Message} when retreiving list of sources from current scene collection!");
            }
        }
    }
}