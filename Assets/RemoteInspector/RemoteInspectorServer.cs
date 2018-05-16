using System;
using UnityEngine;
using WebSocketSharp.Server;

namespace RemoteInspector
{
    public class RemoteInspectorServer : IDisposable
    {
        private const string LogPrefix = "[RemoteInspector] ";

        private readonly HttpServer _server;

        private readonly RequestsProcessor _requestsProcessor = new RequestsProcessor();

        public bool IsRunning
        {
            get { return _server != null && _server.IsListening; }
        }

        public RemoteInspectorServer( ushort port = 8080 )
        {
            _server = new HttpServer( port );
            _server.KeepClean = true;
            _server.ReuseAddress = true;
            _server.OnGet += HandleRequest;
            _server.OnPost += HandleRequest;
        }

        public void Start()
        {
            if ( IsRunning )
            {
                LogError( "Cannot start Remote Inspector server since it is already running" );
                return;
            }

            try
            {
                Log( "Starting Remote Inspector server" );
                _server.Start();
                Log( "Remote Inspector server started" );
            }
            catch ( Exception e )
            {
                LogError( "An exception occured during Remote Inspector server setup." );
                LogException( e );
            }
        }

        public void Stop()
        {
            if ( !IsRunning )
            {
                LogError( "Cannot stop Remote Inspector server since it is not running" );
                return;
            }

            try
            {
                Log( "Stopping Remote Inspector server" );
                _server.Stop();
                Log( "Remote Inspector server properly shut down" );
            }
            catch ( Exception e )
            {
                LogError( "An exception occured during Remote Inspector server stop." );
                LogException( e );
            }
        }

        public void Dispose()
        {
            if ( IsRunning )
            {
                Stop();
            }
        }

        private void HandleRequest( object sender, HttpRequestEventArgs eventArgs )
        {
            var request = eventArgs.Request;
            var response = eventArgs.Response;

            try
            {
                _requestsProcessor.ProcessRequest( request, response );
            }
            catch ( Exception exception )
            {
                LogException( exception );                
            }
        }

        private static void Log( string message )
        {
            Debug.Log( LogPrefix + message );
        }

        private static void LogError( string message )
        {
            Debug.LogError( LogPrefix + message );
        }

        private static void LogException( Exception e )
        {
            var disposeExc = e as ObjectDisposedException;
            if ( disposeExc != null && disposeExc.ObjectName == "listener" )
            {
                // happens when stopping unity editor play mode
                return;
            }

            Debug.LogException( e );
        }
    }
}