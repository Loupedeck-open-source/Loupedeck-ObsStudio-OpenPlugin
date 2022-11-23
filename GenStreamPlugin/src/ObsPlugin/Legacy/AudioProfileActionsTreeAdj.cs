namespace Loupedeck.ObsStudioPlugin.DynamicActions
{
    using System;
    using System.Collections.Generic;

    public class AudioProfileActionsTreeAdj : PluginDynamicAdjustment
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;
        public AudioProfileActionsTreeAdj()
            : base(true, DeviceType.None)
        {
            this.Name = "LegacySpecialSources";
            this.DisplayName = "";
            this.Description = "";
            this.GroupName = "";

        }

        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            var val = "0";

            if(this.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key) && this.Proxy.CurrentAudioSources.ContainsKey(key.Source))
            {
                var volume = (Int32)(this.Proxy.CurrentAudioSources[key.Source].Volume * 100);
                val = $"{volume}%";
            }
            // $ObsStudio___Loupedeck.ObsStudioPlugin.DynamicActions.AudioProfileActionsTreeAdj___0|Browser|Studio

            //Here we need to parse legacy stuff. 

            //  => SceneItemKey.TryParse(actionParameter, out var key) ? key.Source : SourceNameUnknown;
            return val;
        }
            

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            if (this.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key) && this.Proxy.CurrentAudioSources.ContainsKey(key.Source))
            {
                this.Proxy.AppSetVolume(key.Source, diff);
                this.AdjustmentValueChanged();
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key) && this.Proxy.CurrentAudioSources.ContainsKey(key.Source))
            {
                this.Proxy.AppToggleMute(key.Source);
            }
        }

        protected override String GetAdjustmentValue(String actionParameter)
        {
            var volume = "N/A";

            if (this.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key) && this.Proxy.CurrentAudioSources.ContainsKey(key.Source))
            {
                var number = this.Proxy.AppGetVolume(key.Source) * 100.0F;
                volume = number.ToString("0.");
            }

            return volume;
        }
    }
}
