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

        public SourceMuteStateChangedCallback AppEvtSourceMuteStateChanged;
        public SourceVolumeChangedCallback AppEvtSourceVolumeChanged;
        
        public class AudioSourceDesc
        {
            public Boolean SpecialSource; 
            public Boolean Muted;
            public Single Volume;
            public AudioSourceDesc(String _name, OBSWebsocket _that, Boolean isSpecSource=false) 
            { 
               
                this.Muted = false;
                this.Volume = 0;
                this.SpecialSource = isSpecSource;

                try
                {
                    var v = _that.GetVolume(_name);
                    this.Muted = v.Muted;
                    this.Volume = v.Volume;
                }  
                catch (Exception ex)
                {
                    Tracer.Trace($"Exception {ex.Message} getting volume information for source {_name}");
                }
            }
        };

        public Dictionary<String, AudioSourceDesc> currentAudioSources = new Dictionary<String, AudioSourceDesc>();

        public Dictionary<String, String> specialSources = new Dictionary<String, String>();

        private readonly List<String> AudioSourceTypes = new List<String>();

        //this.Trace($"Source Settings: Name: {settings.SourceName}, Kind {settings.SourceKind}, Type {settings.SourceType}.  IS AUDIO {this.AudioSourceTypes.Contains(settings.SourceType)}");
        private Boolean IsAudioSourceType(OBSWebsocketDotNet.Types.SourceSettings settings) => this.AudioSourceTypes.Contains(settings.SourceKind ?? settings.SourceType);

        public delegate void AppSourceCreatedCb(String sourceName);
        public AppSourceCreatedCb AppEvtSourceCreated;
        public AppSourceCreatedCb AppEvtSourceDestroyed;

        private void OnObsSourceCreated(OBSWebsocket sender, OBSWebsocketDotNet.Types.SourceSettings settings)
        {
            //Check if we should care
            if(this.IsAudioSourceType( settings ))
            {
                var src = new OBSWebsocketDotNet.Types.SourceInfo();
                src.Name = settings.SourceName;
                src.TypeID = settings.SourceType;
                this.currentAudioSources[src.Name] = new AudioSourceDesc(src.Name, this);
                this.AppEvtSourceCreated?.Invoke(src.Name);
            }
        }

        private void OnObsSourceDestroyed(OBSWebsocket sender, String sourceName, String sourceType, String sourceKind)
        {
            if(this.currentAudioSources.ContainsKey(sourceName))
            {
                this.currentAudioSources.Remove(sourceName);
                this.AppEvtSourceDestroyed?.Invoke(sourceName);
            }
            else
            {
                this.Trace($"SourceDestroyed: Source {sourceName} is not found in audioSources");
            }
        }

        
        private void OnObsSourceVolumeChanged(OBSWebsocket sender, OBSWebsocketDotNet.Types.SourceVolume volDesc)
        {
            if(! Helpers.TryExecuteAction(()=> this.currentAudioSources[volDesc.SourceName].Volume = volDesc.Volume))
            {
                this.Trace($"OBS: Error updating volume to {volDesc?.Volume} for source {volDesc?.SourceName}");
            }

            this.AppEvtSourceVolumeChanged?.Invoke(sender, volDesc);
        }

        private void OnObsSourceMuteStateChanged(OBSWebsocket sender, String sourceName, Boolean isMuted)
        {
            if (!Helpers.TryExecuteAction(() => this.currentAudioSources[sourceName].Muted = isMuted))
            {
                this.Trace($"OBS: Error setting muted state to {isMuted} for source {sourceName}");
            }
           
            this.AppEvtSourceMuteStateChanged?.Invoke(sender, sourceName, isMuted);
        }

        //NOTE: We are NOT going to OBS for mute and volume, using cached value instead -- This is for LD UI
        public Boolean AppGetMute(String sourceName) =>
            this.IsAppConnected & this.currentAudioSources.ContainsKey(sourceName) ? this.currentAudioSources[sourceName].Muted : false;

        public Single AppGetVolume(String sourceName) =>
            this.IsAppConnected & this.currentAudioSources.ContainsKey(sourceName) ? this.currentAudioSources[sourceName].Volume : (Single)0.0;

        //Toggles mute on the source, returns current state of the mute. 
        public void AppToggleMute(String sourceName)
        {
            if(this.IsAppConnected && this.currentAudioSources.ContainsKey(sourceName))
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
            if (this.IsAppConnected && this.currentAudioSources.ContainsKey(sourceName))
            {
                try
                {
                    var current = this.AppGetVolume(sourceName) + (Single) diff_ticks / 100.0F;

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

        //Once, upon connection
        public void OnAppConnected_RetreiveSourceTypes()
        {
            this.AudioSourceTypes.Clear();            

            if(!Helpers.TryExecuteAction( () =>
                {
                    foreach (var type in this.GetSourceTypesList())
                    {
                        if (type.Capabilities.HasAudio)
                        {
                            this.AudioSourceTypes.Add(type.TypeID);
                            this.Trace($"Type {type.TypeID} will be handled as audio type");
                        }
                    }
                }))
            {
                this.Trace($"Warning: Cannot get list of supported audio types from OBS");
            }
        }
        public void OnAppConnected_RetreiveSpecialSources()
        {
            this.specialSources.Clear();

            if (Helpers.TryExecuteFunc(() => this.GetSpecialSources(), out var sources))
            {
                foreach (var source in sources)
                {
                    this.specialSources.Add(source.Key,source.Value);
                    this.Trace($"Adding Special source {source.Key} Val {source.Value}");
                }
            }
            else
            {
                this.Trace($"Warning: Cannot retreive list of special sources");
            }
        }

        
        //Retreive audio sources from current collection
        private void OnObsSceneCollectionChanged_RetreiveAudioSources()
        {
            this.currentAudioSources.Clear();
            try
            {
                foreach (var specSource in this.specialSources)
                {
                    this.currentAudioSources.Add(specSource.Key, new AudioSourceDesc(specSource.Key, this, true));
                    this.Trace($"Adding General audio source {specSource.Key}");
                }
                
                foreach (var source in this.GetSourcesList())
                {
                    //NOTE: Special sources are seen as in GetSourcesList too! (they're present as 'value' specSource.Value
                    if (!this.specialSources.ContainsValue(source.Name) && this.IsAudioSourceType(this.GetSourceSettings(source.Name)))
                    {
                        //Adding audio source and populating initial values
                        this.currentAudioSources.Add(source.Name, new AudioSourceDesc(source.Name, this));
                        this.Trace($"Adding Regular audio source {source.Name}");
                    }
                }
            }
            catch ( Exception ex )
            {
                //FIXME: Add Plugin Status -> Error here and to similar places
                this.Trace($"Warning: Exception {ex.Message} when retreiving list of sources from current scene collection!");
            }
        }
    }
}
