namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using OBSWebsocketDotNet;

    internal class AudioSourceDescriptor
    {
        public Boolean SpecialSource;
        public Boolean Muted;
        public Single Volume;

        public AudioSourceDescriptor(String name, OBSWebsocket that, Boolean isSpecSource = false)
        {
            this.Muted = false;
            this.Volume = 0;
            this.SpecialSource = isSpecSource;

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
