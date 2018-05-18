using WebSocketSharp.Net;

namespace RemoteInspector.Server
{
    public interface IRequestHandler
    {
        bool HandleRequest( HttpListenerRequest request, HttpListenerResponse response, string relativePath );
    }
}