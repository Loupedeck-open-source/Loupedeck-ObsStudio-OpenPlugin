namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;

    class GenToggle1Command : PluginMultistateDynamicCommand
    {
        private const String StateDisabled = "Disabled";
        private const String StateOn = "On";
        private const String StateOff = "Off";
        enum StateIndex
        {
            STATE_DISABLED = 0,
            STATE_ON,
            STATE_OFF
        };

        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;
        public GenToggle1Command() : base("Toggles toggle1", "Toggles toggle 1 on or oww", /*no group*/"")
        {
            this.AddState(StateDisabled, "Command unavailable");
            this.AddState(StateOff, "Toggle On");   // When in this state, Toggle1 is is off
            this.AddState(StateOn, "Toggle Off");   // When in this state, Toggle1 is is on
        }

        protected override Boolean OnLoad()
        {
            this.Proxy.EvtAppConnected += this.OnAppConnected;
            this.Proxy.EvtAppDisconnected += this.OnAppDisconnected;
            this.Proxy.AppEvtGenToggle1Off += this.AppEvtGenToggle1Off;
            this.Proxy.AppEvtGenToggle1On += this.AppEvtGenToggle1On;

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.EvtAppConnected -= this.OnAppConnected;
            this.Proxy.EvtAppDisconnected -= this.OnAppDisconnected;
            this.Proxy.AppEvtGenToggle1Off -= this.AppEvtGenToggle1Off;
            this.Proxy.AppEvtGenToggle1On -= this.AppEvtGenToggle1On;

            return true;
        }

        private void OnAppConnected(Object sender, EventArgs e)
        {
            //Requesting toggle state
            if (this.Proxy.IsGenToggle1On)
            {
                this.AppEvtGenToggle1On(sender, e);
            }
            else
            {
                this.AppEvtGenToggle1Off(sender, e);
            }
        }

        private void OnAppDisconnected(Object sender, EventArgs e) => this.SetCurrentState((Int32)StateIndex.STATE_DISABLED);
        private void AppEvtGenToggle1Off(Object sender, EventArgs e) => this.SetCurrentState((Int32)StateIndex.STATE_OFF);
        private void AppEvtGenToggle1On(Object sender, EventArgs e) => this.SetCurrentState((Int32)StateIndex.STATE_ON);

        protected override void RunCommand(String actionParameter)
        {
            if ( this.TryGetCurrentStateIndex(out var index))
            {
                if( index == (Int32)StateIndex.STATE_ON )
                {
                    this.Proxy.AppGenericToggle1Off();
                }
                else if (index == (Int32)StateIndex.STATE_OFF)
                {
                    this.Proxy.AppGenericToggle1On();
                }
            }
        }

        private readonly String[] stateIcons =
        {
            "Loupedeck.GenStreamPlugin.icons.Toggle1Disabled.png",
            "Loupedeck.GenStreamPlugin.icons.Toggle1Off.png",
            "Loupedeck.GenStreamPlugin.icons.Toggle1On.png"
        };

        protected override BitmapImage GetCommandImage(String actionParameter, Int32 stateIndex, PluginImageSize imageSize) => EmbeddedResources.ReadImage(this.stateIcons[stateIndex]);
    }
}
