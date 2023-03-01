namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    public abstract class GenericOnOffSwitch : PluginTwoStateDynamicCommand
    {
        public Byte[] OnStateImage { get; private set; }
        public Byte[] OffStateImage { get; private set; }

        public String OnStateName { get; private set; }
        public String OffStateName { get; private set; }

        public void SwitchOn() => this.RunCommand(TwoStateCommand.TurnOff);
        public void SwitchOff() => this.RunCommand(TwoStateCommand.TurnOn);

        public GenericOnOffSwitch(
            String name,
            String displayName, String description, String groupName,
            String offStateName, String onStateName,
            String offStateImage, String onStateImage):base(displayName,description,groupName)
        {
            this.Name = name;
            this.OnStateImage = EmbeddedResources.ReadBinaryFile(ObsStudioPlugin.ImageResPrefix + onStateImage);
            this.OffStateImage = EmbeddedResources.ReadBinaryFile(ObsStudioPlugin.ImageResPrefix + offStateImage);
            this.OnStateName = onStateName;
            this.OffStateName = offStateName;

            var p = this.AddToggleCommand(
                displayName,
                this.OnStateImage.ToImage(),
                this.OffStateImage.ToImage());
            p.Description = description;
            
            this.SetOffStateDisplayName(this.OffStateName);
            this.SetOnStateDisplayName(this.OnStateName);

            UniversalStateSwitch.RegisterToggle(this);
        }

        protected override Boolean OnLoad()
        {
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            this.IsEnabled = false;
            this.ConnectAppEvents(this.AppEvtTurnedOn, this.AppEvtTurnedOff);

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            this.DisconnectAppEvents(this.AppEvtTurnedOn, this.AppEvtTurnedOff);

            return true;
        }

        /// <summary>
        /// Connects command's On and Off events to the source (application)
        /// </summary>
        /// <param name="eventSwitchedOff">Event fired AFTER switch is turned off</param>
        /// <param name="eventSwitchedOn">Event fired AFTER switch is turned on</param>
        protected abstract void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn);

        /// <summary>
        /// Disconnects command's On and Off events to the source (application)
        /// </summary>
        /// <param name="eventSwitchedOff">Event fired AFTER switch is turned off</param>
        /// <param name="eventSwitchedOn">Event fired AFTER switch is turned on</param>
        protected abstract void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn);

        private void OnAppConnected(Object sender, EventArgs e)
        {
            this.IsEnabled = true;
            this.AppEvtTurnedOff(sender, e); // Setting off by default
        }

        private void OnAppDisconnected(Object sender, EventArgs e) => this.IsEnabled = false;


        private void AppEvtTurnedOff(Object sender, EventArgs e)
        {
            this.Plugin.Log.Info($"Action {this.Name}: Setting state to OFF");
            this.TurnOff();
        }

        private void AppEvtTurnedOn(Object sender, EventArgs e)
        {
            this.Plugin.Log.Info($"Action {this.Name}: Setting state to ON");
            this.TurnOn();
        }

        protected override void RunCommand(String actionParameter)
        {
            //To ensure legacy commands are not executed
            if (actionParameter != null)
            {
                base.RunCommand(actionParameter);
            }
        }
    }
}
