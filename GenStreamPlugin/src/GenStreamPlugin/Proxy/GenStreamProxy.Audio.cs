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

        public List<OBSWebsocketDotNet.Types.SourceInfo> currentAudioSources = new List<OBSWebsocketDotNet.Types.SourceInfo>();
        public Dictionary<String, String> specialSources = new Dictionary<String, String>();

        private readonly List<String> AudioSourceTypes = new List<String>();
        private Boolean IsAudioSourceType(String type) => this.AudioSourceTypes.Contains(type);


        public void OnObsSourceVolumeChanged(OBSWebsocket sender, OBSWebsocketDotNet.Types.SourceVolume volDesc)
        {
            this.Trace($"OBS: Source {volDesc?.SourceName} volume changed to {volDesc?.Volume}");


            //FIXME
            //Based on the source name, we will route it to either 'normal audio source' or to 'general audio source'
            
            


            //Note, setting VolumeByObs, which will reset 'is changed' flag. 
            if (!Helpers.TryExecuteAction(() => this.allSceneItems[SceneItemKey.Encode(this.CurrentSceneCollection, this.CurrentScene.Name, volDesc.SourceName)].VolumebyObs = volDesc.Volume))
            {
                this.Trace($"Warning: Cannot update volume for source { volDesc?.SourceName} current scene.");
            }
            else
            {
                this.AppEvtSourceVolumeChanged?.Invoke(sender, volDesc);
            }
        }

        void OnObsSourceMuteStateChanged(OBSWebsocket sender, String sourceName, Boolean isMuted)
        {
            this.Trace($"OBS: Source {sourceName} mute state changed to {isMuted}");

            //Note, setting MutedByObs, which will reset 'is changed' flag. 
            if (!Helpers.TryExecuteAction(() => this.allSceneItems[SceneItemKey.Encode(this.CurrentSceneCollection, this.CurrentScene.Name, sourceName)].MutedByObs = isMuted))
            {
                this.Trace($"Warning: Cannot update mute state for source {sourceName} current scene.");
            }
            else
            {
                this.AppEvtSourceMuteStateChanged?.Invoke(sender, sourceName, isMuted);
            }
        }

        private void SyncAudioStateWithOBS()
        {

            //When changing current scene, make sure that
            //  (a) if user has changed mute status  (AppTogglemute) for sources in other scene, it is set correctly
            //  (b) those sources that have not been modified by user needs to get an up-to-date Mute status from OBS
            foreach (var item in this.allSceneItems)
            {
                if (!item.Value.SceneName.Equals(this.CurrentScene.Name) || !item.Value.Is_volume_controlled)
                {
                    continue;
                }

                //Doin' nothin' for current scene
                var obsMute = this.GetMute(item.Value.SourceName);
                if (item.Value.MutedByLD != obsMute)
                {
                    //Note. the expectation is that when Mute is actually toggled, we will receive an event 
                    if (item.Value.ObsMuteUpdatePending)
                    {
                        //User has updated mute while source was not there. 
                        this.SetMute(item.Value.SourceName, item.Value.MutedByLD);
                    }
                    else
                    {
                        item.Value.MutedByObs = obsMute;
                    }
                }
                //FIXME: Check float comparison, with 0.1 precision
                var volDesc = this.GetVolume(item.Value.SourceName);
                if (item.Value.VolumeByLD != volDesc.Volume)
                {
                    if (item.Value.ObsVolumeUpdatePending)
                    {
                        //User has updated mute while source was not there. 
                        this.SetVolume(item.Value.SourceName, item.Value.VolumeByLD);
                    }
                    else
                    {
                        item.Value.VolumebyObs = volDesc.Volume;
                    }
                }
            }

        }

        public void AppToggleMute(String sourceName)
        {
            // For all soureces, we swtching our mute flag
            var key = SceneItemKey.Encode(this.CurrentSceneCollection, sceneName, sourceName);

            this.allSceneItems[key].MutedByLD = !this.allSceneItems[key].MutedByLD;

            if (sceneName.Equals(this.CurrentScene?.Name) && this.allSceneItems[key].ObsMuteUpdatePending)
            {
                // We switching mute only for the current scene 
                this.SetMute(sourceName, this.allSceneItems[key].MutedByLD);
            }
        }
        public void AppSetVolume(String sceneName, String sourceName, Single volume)
        {
            if (!Helpers.TryExecuteAction(() => this.SetVolume(sourceName, volume)))
            {
                this.Trace($"Warning! Cannot set volume of source {sourceName} to {volume}");
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
                    this.specialSources[source.Key] = source.Value;
                }
            }
            else
            {
                this.Trace($"Warning: Cannot retreive list of special sources");
            }
        }

        //SourceInfo
        // Volume
        // Audio 

        
        //Retreive audio sources from current collection
        public void OnObsSceneCollectionChanged_RetreiveAudioSources()
        {
            this.currentAudioSources.Clear();

            if (Helpers.TryExecuteFunc(() => this.GetSourcesList(), out var sources))
            {
                foreach (var source in sources)
                {
                    if (this.IsAudioSourceType(source.TypeID))
                    {
                        this.currentAudioSources.Add(source);
                    }
                }
            }
            else
            {
                this.Trace($"Warning: Cannot retreive list of sources from current scene collection!");
            }
        }

        // On Scene collection 
/*
        GetSourcesList

        GetSourceTypesList
        types.*.caps.hasAudio

        GetSpecialSources
*/

    }
}
