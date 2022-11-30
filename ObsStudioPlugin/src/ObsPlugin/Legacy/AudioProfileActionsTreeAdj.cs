namespace Loupedeck.ObsStudioPlugin.DynamicActions
{
    using System;

    internal class AudioProfileActionsTreeAdj : PluginDynamicAdjustment
    {
        
        public AudioProfileActionsTreeAdj()
            : base(true, DeviceType.None)
        {
            this.DisplayName = "";
            this.Description = "";
            this.GroupName = "";

        }

        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            var val = "0";

            if(ObsStudioPlugin.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key) && ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(key.Source))
            {
                var volume = (Int32)(ObsStudioPlugin.Proxy.CurrentAudioSources[key.Source].Volume * 100);
                val = $"{volume}%";
            }
            // $ObsStudio___Loupedeck.ObsStudioPlugin.DynamicActions.AudioProfileActionsTreeAdj___0|Browser|Studio

            //Here we need to parse legacy stuff. 

            //  => SceneItemKey.TryParse(actionParameter, out var key) ? key.Source : SourceNameUnknown;
            return val;
        }
            

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            if (ObsStudioPlugin.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key) && ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(key.Source))
            {
                ObsStudioPlugin.Proxy.AppSetVolume(key.Source, diff);
                this.AdjustmentValueChanged();
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (ObsStudioPlugin.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key) && ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(key.Source))
            {
                ObsStudioPlugin.Proxy.AppToggleMute(key.Source);
            }
        }

        protected override String GetAdjustmentValue(String actionParameter)
        {
            var volume = "N/A";

            if (ObsStudioPlugin.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key) && ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(key.Source))
            {
                var number = ObsStudioPlugin.Proxy.AppGetVolume(key.Source) * 100.0F;
                volume = number.ToString("0.");
            }

            return volume;
        }
    }
}
