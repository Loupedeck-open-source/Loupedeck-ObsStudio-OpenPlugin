namespace Loupedeck.GenStreamPlugin
{
    using System;


    public class GenStreamProxy
    {
        // Our 'own' events       
        public event EventHandler<EventArgs> EvtAppConnected;
        public event EventHandler<EventArgs> EvtAppDisconnected;

        //Event forwarded from App.
        // Toggle1 Toggled on in target application
        public event EventHandler<EventArgs> AppEvtGenToggle1On;
        // Toggle1 Toggled off in target application
        public event EventHandler<EventArgs> AppEvtGenToggle1Off;

        // Properties
        public Boolean IsAppConnected =>  this._app != null; 
        public Object _app { get; private set; }

        public GenStreamProxy()
        {
            this._app = null;
            this.EvtAppConnected += this.OnAppConnected;
            this.EvtAppDisconnected += this.OnAppDisconnected;
        }
        ~GenStreamProxy()
        {
            this.EvtAppConnected -= this.OnAppConnected;
            this.EvtAppDisconnected -= this.OnAppDisconnected;
        }

        //Requests state from the app 
        public Boolean IsGenToggle1On 
        { get {
                if (this.IsAppConnected)
                {
                    // Requesting state from app
                    // _app->xxxxx
                    return true;
                }

                return false;
            } 
        }

        // Commands
        public void AppGenericToggle1On() 
        {
/*          if (this.IsAppConnected)
            {
                Helpers.TryExecuteAction(() =>
                    {
                       _app.DoSomething()
                    }
                );
            }
*/
        }

        public void AppGenericToggle1Off()
        {
            /*          if (this.IsAppConnected)
                        {
                            Helpers.TryExecuteAction(() =>
                                {
                                   _app.DoSomething()
                                }
                            );
                        }
            */
        }

        // Event forwarders
        private void OnAppToggle1On() => this.AppEvtGenToggle1On?.Invoke(this, new EventArgs());
        private void OnAppToggle1Off() => this.AppEvtGenToggle1Off?.Invoke(this, new EventArgs());

        private void OnAppConnected(Object sender, EventArgs e)
        {
            // Subscribing to App events
            // _app.Toggle1On += OnAppToggle1On;
            // _app.Toggle1Off += OnAppToggle1Off;

        }
        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            // Unsubscribing from App events here
        }

        public Boolean AttemptConnecting()
        {
            if (this.IsAppConnected)
            {
                return true;
            }

            try
            {
                Tracer.Warning($"GenStreamPlugin: Connecting..");
                
                this.EvtAppConnected?.Invoke(this, new EventArgs());

            }
            catch (Exception ex)
            {
                Tracer.Error($"GenStreamPlugin: Exception disconnecting :{ex.Message}, inner {ex.InnerException?.Message}");
            }

            return this.IsAppConnected;
        }

        public void Connect()
        {
            this.AttemptConnecting();
        }

        public void Disconnect()
        {
            if (!this.IsAppConnected)
            {
                return;
            }

            try
            {
                this.EvtAppDisconnected?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                Tracer.Error($"GenStreamPlugin: Exception :{ex.Message}, inner {ex.InnerException?.Message}");
            }

            this._app = null;
        }
    }
}
