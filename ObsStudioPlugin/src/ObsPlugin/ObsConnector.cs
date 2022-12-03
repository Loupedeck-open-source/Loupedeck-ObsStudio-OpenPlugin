/**
     OBS Connection logic overview

     All connection logic is centered around the 'port file' - a file in OBS PluginData directory that
     contains a websocket port number to connect to.  The OBS-side plugin creates this file just when the websocket
     server starts listening.  When websocket server is closed gracefully (or just before creating a new file)
     the port file is delteted.

    This class sets up a File System Watcher to monitor file creation of 'websocket.port' file in the given Plugin Data drirectory and
    once file creation event fires, tries to establish a connection to OBS studio using the given port

    The Connector is started using 'Start' method and stopped using 'Stop'

    To address the case where OBS studio is started _before_ Loupedeck,  unconditional connection attempt is made
    on the start of the connector.

    To prevent several parallel connection attempts (for example, if OBS is quicly restarted after crash),
    if there is a concurrent connection attempt detected, it is cancelled and special 5 seconds timer is started to re-try an attempt later.
    The same logic would apply to all the other attempts so there should not be two concurrent OBS 'Connect' calls
    The optional (non-blocking) OnConnecting callback (passed in constructor) is just before a connection attempt is made.
    All other connection progress can be monitored using OBS built-in OnConnected and OnDisconnected events.
    */
namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using OBSWebsocketDotNet;

    /// <summary>
    /// OBS conneection monitoring class, counterpart of Loupedeck OBS plugin
    /// </summary>
    internal class ObsConnector : IDisposable
    {
        private readonly String _port_file_path;
        private readonly OBSWebsocket _obs;
        private readonly FileSystemWatcher _fs_watcher;
        private readonly System.Timers.Timer _connect_retry_timer;

        private readonly Object _connecting_lock = new Object();
        private Boolean _connecting;

        private Boolean IsConnecting
        {
            get
            {
                lock (this._connecting_lock)
                {
                    return this._connecting;
                }
            }

            set
            {
                lock (this._connecting_lock)
                {
                    this._connecting = value;
                }
            }
        }

        public ObsConnector(OBSWebsocket obs, String plugin_data_directory, EventHandler<EventArgs> connectingCallback)
        {
            this._obs = obs;
            this.ConnectingCallback = connectingCallback;

            _ = IoHelpers.EnsureDirectoryExists(plugin_data_directory);
            this._fs_watcher = new FileSystemWatcher(plugin_data_directory);
            this._fs_watcher.NotifyFilter = NotifyFilters.CreationTime
                               | NotifyFilters.FileName
                               | NotifyFilters.LastAccess
                               | NotifyFilters.LastWrite
                               | NotifyFilters.Size;

            // We only monitor Created event, assuming that the port file is deleted just before creation
            this._fs_watcher.Created += this.OnFSWatcherEvent;
            this._fs_watcher.Error += this.OnFSWatcherError;
            this._fs_watcher.IncludeSubdirectories = false;

            this._fs_watcher.Filter = "websocket.port";
            this._port_file_path = Path.Combine(plugin_data_directory, this._fs_watcher.Filter);
            this._fs_watcher.EnableRaisingEvents = false;

            this._connect_retry_timer = new System.Timers.Timer(5000);
            this._connect_retry_timer.AutoReset = false;
            this._connect_retry_timer.Elapsed += (e, s) => this.OnFSWatcherEvent(null, null);

            this._connect_retry_timer.Enabled = false;
        }

        /// <summary>
        /// Triggered when connection attempt is being made
        /// </summary>
        public event EventHandler<EventArgs> ConnectingCallback;

        public void Start()
        {
            this._fs_watcher.EnableRaisingEvents = true;

            // Attempting to connect right away
            _ = Task.Run(() => Helpers.TryExecuteSafe(() => this.Connect()));
        }

        public void Stop() =>

            // Unregister to OBS Events
            this._fs_watcher.EnableRaisingEvents = false;

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this._fs_watcher.Dispose();
            this._connect_retry_timer.Dispose();
        }

        private void EnableReconnection()
        {
            // Starting the timer
            this._connect_retry_timer.Enabled = true;
            Tracer.Trace($"OBS: Re-trying connection in {this._connect_retry_timer.Interval} ms ");
        }

        private void Connect()
        {
            if (!File.Exists(this._port_file_path))
            {
                Tracer.Warning($"OBS: Cannot connect, port file \"{this._port_file_path}\" does not exist");
                return;
            }
            else if (this._obs.IsConnected)
            {
                Tracer.Warning($"OBS: Already connected, ignoring");
                return;
            }

            if (!Int32.TryParse(IoHelpers.ReadTextFile(this._port_file_path), out var wsPort))
            {
                Tracer.Error($"OBS: Error connecting, cannot read port from file \"{this._port_file_path}\", re-trying");

                // Assuming this is an interminnent error, we re-try
                this.EnableReconnection();
            }
            else
            {
                if (this.IsConnecting)
                {
                    // If we came here WHILE there is connection attempt in progress (in another thread)
                    // Arming a timer that would re-run OnFSWatcherEvent
                    this.EnableReconnection();

                    return;
                }
                else
                {
                    this.IsConnecting = true;
                }

                // Actual connection
                var obs_ws_conn = $"ws://localhost:{wsPort}";

                Tracer.Trace($"OBS: connecting to '{obs_ws_conn}' ");

                this.ConnectingCallback?.Invoke(this, new EventArgs());

                var password = Loupedeck.AesString.Decrypt("EAAAAOVpn/mqFwbWixg6hzsxeBiUjf+BBZTmfrYzLFgUWMV0", "FourtyTwo");

                if (!Helpers.TryExecuteAction(() => this._obs.Connect(obs_ws_conn, password)))
                {
                    Tracer.Error("OBS: Error connecting to OBS");
                }

                this.IsConnecting = false;
            }
        }

        private void OnFSWatcherEvent(Object sender, FileSystemEventArgs args)
        {
            // Since we came here from 'File Created' event handler, we sleep to ensure the file
            // is written and unlocked on OBS side
            Thread.Sleep(1000);

            this.Connect();
        }

        private void OnFSWatcherError(Object sender, ErrorEventArgs e) => Tracer.Error(e?.GetException(), $"OBS: File system watcher error {e}");
    }
}