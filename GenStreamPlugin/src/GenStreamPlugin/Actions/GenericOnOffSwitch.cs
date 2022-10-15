namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;

    abstract class GenericOnOffSwitch : PluginMultistateDynamicCommand
    {
        enum StateIndex
        {
            STATE_DISABLED = 0,
            STATE_ON,
            STATE_OFF
        };

        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;

        private readonly String[] stateIcons;

        /// <summary>
        /// Creates a generic switch. 
        /// </summary>
        /// <param name="displayName">Command Display Name</param>
        /// <param name="description">Description in Action Editor</param>
        /// <param name="groupName">Group name</param>
        /// <param name="stateNames">Array of 3 strings with Disabled, Off and On state names </param>
        /// <param name="stateImages">Array of 3 strings with Disabled, Off and On state images </param>
        /// 
        public GenericOnOffSwitch(String displayName, String description, String groupName, String [] stateNames, String [] stateImages) 
            : base(displayName, description, groupName)
        {
            if( stateNames == null || stateNames.Length !=3 || stateImages == null || stateImages.Length != 3 )
            {
                throw new ArgumentException("Cannot create Generic switch: Invalid state or images array");
            }

            this.AddState("Disabled", stateNames[0]);
            this.AddState("Off",      stateNames[1]);   // When in this state, toggle is is off
            this.AddState("On",       stateNames[2]);   // When in this state, toggle is is on
            this.stateIcons = stateImages;
        }

        protected override Boolean OnLoad()
        {
            this.Proxy.EvtAppConnected += this.OnAppConnected;
            this.Proxy.EvtAppDisconnected += this.OnAppDisconnected;

            this.ConnectAppEvents(this.AppEvtTurnedOn, this.AppEvtTurnedOff);

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.EvtAppConnected -= this.OnAppConnected;
            this.Proxy.EvtAppDisconnected -= this.OnAppDisconnected;

            this.DisconnectAppEvents(this.AppEvtTurnedOn, this.AppEvtTurnedOff);

            return true;
        }
        /// <summary>
        /// Connects command's On and Off events to the source (application)
        /// </summary>
        
        /// <param name="OnEvent">Event when the switch is turned on</param>
        /// <param name="OffEvent">Event when the switch is turned off</param>
        protected abstract void ConnectAppEvents(EventHandler<EventArgs> OnEvent, EventHandler<EventArgs> OffEvent);
        /// <summary>
        /// Disconnects command's On and Off events to the source (application)
        /// </summary>
        /// <param name="OnEvent">Event when the switch is turned on</param>
        /// <param name="OffEvent">Event when the switch is turned off</param>
        protected abstract void DisconnectAppEvents(EventHandler<EventArgs> OnEvent, EventHandler<EventArgs> OffEvent);

        private void OnAppConnected(Object sender, EventArgs e) => this.AppEvtTurnedOff(sender, e); //Setting off by default
        private void OnAppDisconnected(Object sender, EventArgs e) => this.SetCurrentState((Int32)StateIndex.STATE_DISABLED);
        private void AppEvtTurnedOff(Object sender, EventArgs e) => this.SetCurrentState((Int32)StateIndex.STATE_OFF);
        private void AppEvtTurnedOn(Object sender, EventArgs e) => this.SetCurrentState((Int32)StateIndex.STATE_ON);

        protected override BitmapImage GetCommandImage(String actionParameter, Int32 stateIndex, PluginImageSize imageSize) => EmbeddedResources.ReadImage(this.stateIcons[stateIndex]);
    }
}
