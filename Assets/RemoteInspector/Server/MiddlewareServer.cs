using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace RemoteInspector.Server
{
    public class MiddlewareServer : IDisposable
    {
        private readonly List<Middleware> _middlewares = new List<Middleware>();

        private const string LogPrefix = "[RemoteInspector] ";

        private readonly HttpServer _server;

        public bool IsRunning
        {
            get { return _server != null && _server.IsListening; }
        }

        public MiddlewareServer( ushort port = 8080 )
        {
            _server = new HttpServer( port );
            _server.KeepClean = true;
            _server.ReuseAddress = true;
            _server.OnGet += HandleRequest;
            _server.OnPost += HandleRequest;
            _server.OnPut += HandleRequest;
            _server.OnDelete += HandleRequest;
        }

        
        public void Use( IRequestHandler handler )
        {
            Use( "", handler );
        }

        public void Use( Func<HttpListenerRequest, HttpListenerResponse, string, bool> handler )
        {
            Use( "", new RequestHandlerDelegate( handler ) );
        }

        public void Use( string path, Func<HttpListenerRequest, HttpListenerResponse, string, bool> handler )
        {
            Use( path, new RequestHandlerDelegate( handler ) );
        }

        public void Use( string path, IRequestHandler handler )
        {
            _middlewares.Add( new Middleware( path, handler ) );
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
                Log( "Remote Inspector server listening on port : " + _server.Port );
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
            var path = ServerUtility.UrlDecode( request.Url.AbsolutePath );

            try
            {
                var handled = false;
                foreach ( var middleware in _middlewares )
                {
                    if ( path.StartsWith( middleware.MountPoint ) )
                    {
                        var relativePath = path.Substring( middleware.MountPoint.Length );
                        if ( relativePath != "" && relativePath[ 0 ] != '/' ) continue;
                        
                        handled = middleware.Handler.HandleRequest( request, response, relativePath );
                        if ( handled ) break;
                    }
                }

                if ( !handled )
                {
                    response.Send( HttpStatusCode.NotFound );
                }
            }
            catch ( Exception exception )
            {
                LogException( exception );
                response.SendException( exception );
            }
        }

        public static void Log( string message )
        {
            Debug.Log( LogPrefix + message );
        }

        public static void LogError( string message )
        {
            Debug.LogError( LogPrefix + message );
        }

        public static void LogWarning( string message )
        {
            Debug.LogWarning( LogPrefix + message );
        }

        public static void LogException( Exception e )
        {
            var disposeExc = e as ObjectDisposedException;
            if ( disposeExc != null && disposeExc.ObjectName == "listener" )
            {
                // happens when stopping unity editor play mode
                return;
            }

            Debug.LogException( e );
        }

        private struct Middleware
        {
            public readonly string MountPoint;
            public readonly IRequestHandler Handler;

            public Middleware( string mountPoint, IRequestHandler handler )
            {
                MountPoint = mountPoint;
                Handler = handler;
            }
        }
    }
}