using System;
using System.Collections.Generic;
using WebSocketSharp.Net;

namespace RemoteInspector.Server
{
    public class RequestsProcessor
    {
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

        private readonly List<Middleware> _middlewares = new List<Middleware>();

        public RequestsProcessor()
        {
            _middlewares.Add( new Middleware( "/api/", new ApiServer() ) );
            _middlewares.Add( new Middleware( "/", new StaticFileServer( "Assets" ) ) );
        }

        public void ProcessRequest( HttpListenerRequest request, HttpListenerResponse response )
        {
            var path = ServerUtility.UrlDecode( request.Url.AbsolutePath );

            try
            {
                var handled = false;
                foreach ( var middleware in _middlewares )
                {
                    if ( path.StartsWith( middleware.MountPoint ) )
                    {
                        handled = middleware.Handler.HandleRequest( request, response,
                            path.Substring( middleware.MountPoint.Length ) );
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
                response.SendException( exception );
                throw;
            }
        }
    }
}