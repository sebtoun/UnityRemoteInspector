using System;
using WebSocketSharp.Net;

namespace RemoteInspector.Server
{
    public interface IRequestHandler
    {
        bool HandleRequest( HttpListenerRequest request, HttpListenerResponse response, string relativePath );
    }

    public class RequestHandlerDelegate : IRequestHandler
    {
        private readonly Func<HttpListenerRequest, HttpListenerResponse, string, bool> _delegate;

        public RequestHandlerDelegate( Func<HttpListenerRequest, HttpListenerResponse, string, bool> @delegate )
        {
            _delegate = @delegate;
        }

        public bool HandleRequest( HttpListenerRequest request, HttpListenerResponse response,
            string relativePath )
        {
            return _delegate( request, response, relativePath );
        }

        public static implicit operator RequestHandlerDelegate(
            Func<HttpListenerRequest, HttpListenerResponse, string, bool> handler )
        {
            return new RequestHandlerDelegate( handler );
        }
    }
}