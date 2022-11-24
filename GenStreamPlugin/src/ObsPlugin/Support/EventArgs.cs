namespace Loupedeck.ObsStudioPlugin
{
    using System;

    public class MuteEventArgs : EventArgs
    {
        public String SourceName;
        public Boolean isMuted;
        public MuteEventArgs(String name, Boolean muted) 
        { 
            this.SourceName = name; 
            this.isMuted = muted; 
        }
    }

    public class VolumeEventArgs : EventArgs
    {
        public String SourceName;
        public Single Volume;
        public Single VolumeDb;
        public VolumeEventArgs(String name, Single vol, Single vol_db = 0)
        {
            this.SourceName = name;
            this.Volume = vol;
            this.VolumeDb = vol_db;
        }
    }

    //Commonly used old-new arg class for 'onchange' events 
    public class OldNewStringChangeEventArgs : EventArgs
    {
        public String Old;
        public String New;
        public OldNewStringChangeEventArgs(String old, String _new) 
        { 
            this.Old = old; 
            this.New = _new; 
        }
    }

    public class SourceNameEventArgs: EventArgs
    {
        public String SourceName;
        public SourceNameEventArgs(String name) { this.SourceName = name; }

    }
}
