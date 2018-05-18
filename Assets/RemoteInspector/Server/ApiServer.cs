using System.IO;
using UnityEngine;
using WebSocketSharp.Net;

namespace RemoteInspector.Server
{
    public class ApiServer : IRequestHandler
    {
        public bool HandleRequest( HttpListenerRequest request, HttpListenerResponse response, string relativePath )
        {
            Debug.Log( relativePath );
            
            Debug.Log( request.ContentLength64 );
            Debug.Log( new StreamReader( request.InputStream ).ReadToEnd() );
            
            return false;
        }
    }
}