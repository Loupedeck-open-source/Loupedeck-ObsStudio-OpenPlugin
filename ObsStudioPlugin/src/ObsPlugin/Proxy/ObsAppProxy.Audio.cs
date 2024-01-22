namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlTypes;
    using System.Runtime.Remoting.Messaging;
    using System.Web;

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

        private readonly List<String> _audioSourceTypes = new List<String>();

        internal Dictionary<String, AudioSourceDescriptor> CurrentAudioSources { get; private set; }  = new Dictionary<String, AudioSourceDescriptor>();

#if falses
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
#endif
        /// <summary>
        /// Adds a source to CurrentAudioSources list
        /// </summary>
        /// <param name="sourceName">Name of the source</param>
        /// <param name="testAudio">Test if source has audio active</param>
        /// <returns>True if source is added</returns>
        private Boolean AddCurrentAudioSource(String sourceName, Boolean testSettings = true, Boolean testAudio = true)
        {
#if false
            if ( Helpers.TryExecuteFunc(
                        () => (!testSettings)
                                                 && (!testAudio || this.GetAudioActive(sourceName)), out var good) && good)
            {
                this.CurrentAudioSources[sourceName] = new AudioSourceDescriptor(sourceName, this);
                this.Plugin.Log.Info($"Adding Regular audio source {sourceName}");
                return true;
            }
#endif
            return false;

        }


        private void OnObsSourceCreated(Object _, OBSWebsocketDotNet.Types.Events.InputCreatedEventArgs args)
        {
            // Check if we should care
            this.Plugin.Log.Info($"OBS: OnObsSourceCreated '{args.InputName}' kind '{args.InputKind}'");

            /*
            if (this.AddCurrentAudioSource(settings.SourceName, false, true))
            {
                this.AppSourceCreated?.Invoke(this, new SourceNameEventArgs(settings.SourceName));
            }*/
        }

        private void OnObsSourceDestroyed(Object _, OBSWebsocketDotNet.Types.Events.InputRemovedEventArgs args)
        //private void OnObsSourceDestroyed(OBSWebsocket sender, String sourceName, String sourceType, String sourceKind)
        {
            if (this.CurrentAudioSources.ContainsKey(args.InputName))
            {
                this.CurrentAudioSources.Remove(args.InputName);
                //this.AppSourceDestroyed?.Invoke(this, new SourceNameEventArgs(sourceName));
            }
            else
            {
                this.Plugin.Log.Warning($"SourceDestroyed: Source {args.InputName} is not found in audioSources");
            }
        }
        private void OnObsSourceVolumeChanged(Object sender, OBSWebsocketDotNet.Types.Events.InputVolumeChangedEventArgs args)
        {
             
            if (this.CurrentAudioSources.ContainsKey(args.Volume.InputName))
            {
                this.CurrentAudioSources[args.Volume.InputName].Volume = args.Volume.InputVolumeDb;
                this.AppEvtSourceVolumeChanged?.Invoke(sender, new VolumeEventArgs(args.Volume.InputName,args.Volume.InputVolumeMul, args.Volume.InputVolumeDb));
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot update volume of {args?.Volume.InputName} --not present in current dbgSrc");
            }
        }
        private void OnObsSourceMuteStateChanged(Object sender, OBSWebsocketDotNet.Types.Events.InputMuteStateChangedEventArgs args)
        {
            
            if (this.CurrentAudioSources.ContainsKey(args.InputName))
            {
                this.CurrentAudioSources[args.InputName].Muted = args.InputMuted;
                this.AppEvtSourceMuteStateChanged?.Invoke(sender, new MuteEventArgs(args.InputName, args.InputMuted));
                this.Plugin.Log.Info($"OBS: OnObsSourceMuteStateChanged Source '{args.InputName}' is muted '{args.InputMuted}'");
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot update mute status. Source {args.InputName} not in current dbgSrc");
            }
        }

        // NOTE: We are NOT going to OBS for mute and volume, using cached value instead -- This is for LD UI
        internal Boolean AppGetMute(String sourceName) =>
            this.IsAppConnected && this.CurrentAudioSources.ContainsKey(sourceName) && this.CurrentAudioSources[sourceName].Muted;

        // Toggles mute on the source, returns current state of the mute.
        internal void AppToggleMute(String sourceName)
        {
            if (this.IsAppConnected && this.CurrentAudioSources.ContainsKey(sourceName))
            {
                try
                {
                    var mute = this.AppGetMute(sourceName);
                    this.SetInputMute(sourceName, !mute);
                    this.Plugin.Log.Info($"OBS: Setting mute to source '{sourceName}' to '{!mute}'");
                }
                catch (Exception ex)
                {
                    this.Plugin.Log.Error($"Exception {ex.InnerException.Message} -- Cannot set mute for source {sourceName}");
                }
            }
            else
            {
                this.Plugin.Log.Warning($"AppToggleMute: Source {sourceName} not found in current dbgSrc, ignoring");
            }
        }

        private static readonly Double MinVolumeDB = -96.2;
        private static readonly Double MaxVolumeDB = 0.0;
        private static readonly Double MinVolumeStep = 0.1;
        private static readonly Double MaxVolumeStep = 3;
        private Single GetVolumePercent(String sourceName) => (Single)(100.0 * (this.CurrentAudioSources[sourceName].Volume - MinVolumeDB) / (MaxVolumeDB - MinVolumeDB));

        private Double CalculateVolumeStep(Double volume /*Note volme here is 0..1 */) 
            => MinVolumeStep + (MaxVolumeStep - MinVolumeStep) * Math.Log(Math.Exp(1.0) - volume * (Math.Exp(1.0) - 1.0));

        internal void AppSetVolume(String sourceName, Int32 diff_ticks)
        {
            if (!this.IsAppConnected && !this.CurrentAudioSources.ContainsKey(sourceName))
            {
                this.Plugin.Log.Warning($"AppSetVolume: Source {sourceName} not found in current dbgSrc, ignoring");
                return;
            }

            try
            {
                var scaledVol = (this.CurrentAudioSources[sourceName].Volume - MinVolumeDB) / (MaxVolumeDB - MinVolumeDB);
                var step = this.CalculateVolumeStep(scaledVol);
                var current = this.CurrentAudioSources[sourceName].Volume +  (Single) (diff_ticks * step);
               
                current = (Single)(current < MinVolumeDB ? MinVolumeDB : (current > MaxVolumeDB ? MaxVolumeDB : current));

                this.SetInputVolume(sourceName, current, true);
            }
            catch (Exception ex)
            {
                this.Plugin.Log.Error($"Exception {ex.InnerException.Message} -- Cannot set volume for source {sourceName}");
            }
        }

        public String AppGetVolumeLabel(String sourceName)
        {
            //We will return volume as % of MAX(!)
            return (this.IsAppConnected && !String.IsNullOrEmpty(sourceName) && this.CurrentAudioSources.ContainsKey(sourceName)) 
                  ? this.GetVolumePercent(sourceName).ToString("0.")
                  : "N/A";
        }


        // Executed once, upon connection. Note, throws!!
        private void OnAppConnected_RetreiveSourceTypes()
        {
            this._audioSourceTypes.Clear();
            var audioTypes = "";
            foreach (var type in this.GetInputKindList())
            {
                //if (type.Capabilities.HasAudio)
                {
                    this._audioSourceTypes.Add(type);
                    audioTypes += $"{type}, ";
                }
            }

            this.Plugin.Log.Info($"Source Audio Types: {audioTypes}");
        }

        // Retreive audio sources from current collection
        private void OnObsSceneCollectionChanged_RetreiveAudioSources()
        {
            this.CurrentAudioSources.Clear();
            try
            {
                var dbgSrc = "";
                foreach (var input in this.GetInputList())
                {       
                    // Adding audio source and populating initial values
                    this.CurrentAudioSources[input.InputName] = new AudioSourceDescriptor(input.InputName, this);
                    dbgSrc += $"\"{input.InputName}\",";

                    this.Plugin.Log.Info($"Input {input.InputName} is of a kind \"{input.InputKind}\"");

                    var s = this.GetInputSettings(input.InputName);
                    var v = this.GetInputVolume(input.InputName);
                    var m = this.GetInputMute(input.InputName);

                }

                this.Plugin.Log.Info($"Added audio source: {dbgSrc}");

                //Adding special sources
                foreach (var input in this.GetSpecialInputs())
                {
                    this.CurrentAudioSources[input.Key] = new AudioSourceDescriptor(input.Value, this);
                    this.Plugin.Log.Info($"Adding special input {input.Key} is of a kind \"{input.Value}\"");
                }
                    
            }
            catch (Exception ex)
            {
                this.Plugin.Log.Error($"OnObsSceneCollectionChanged_RetreiveAudioSources: Exception '{ex.Message}' when retreiving list of dbgSrc from current scene collection!");
            }
        }
    }
}
