namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

    using OBSWebsocketDotNet;

    internal class AudioSourceDescriptor
    {
        public Dictionary<String, SourceFilter> Filters = new Dictionary<String, SourceFilter>();
        public Boolean SpecialSource;
        public Boolean Muted;
        public Single Volume;
        public String SourceName;

        public AudioSourceDescriptor(String name, OBSWebsocket that, Boolean isSpecSource = false)
        {
            this.Muted = false;
            this.Volume = 0;
            this.SpecialSource = isSpecSource;
            this.SourceName = name;
            Tracer.Trace($"Creating AudioSourceDescriptor for \"{name}\", isSpecialSource={isSpecSource}");
            try
            {   /*NB. All volume in decibels!*/
                var v = that.GetInputVolume(name);
                this.Muted = that.GetInputMute(name);
                this.Volume = v.VolumeDb;
            }
            catch (Exception ex)
            {
                Tracer.Error($"Exception {ex.Message} getting volume information for source {name}");
            }
        }
    }


}
