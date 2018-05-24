using WebSocketSharp.Net;

namespace RemoteInspector.Server
{
    public class StaticFileServer : IRequestHandler
    {
        private readonly string _root;

        public StaticFileServer( string root )
        {
            _root = root;
        }

        public bool HandleRequest( HttpListenerRequest request, HttpListenerResponse response, string relativePath )
        {
            if ( request.HttpMethod != "GET" )
            {
                return false;
            }

            if ( relativePath == "" )
            {
                return false;
            }

            var path = _root + "/" + relativePath;

            var contentType = GuessContentTypeForPath( path );
            byte[] content = null;

            UnityMainThreadDispatcher.Instance().EnqueueAndWait( () =>
            {
                try
                {
                    content = ResourceLoader.GetBinaryFileContent( path );
                }
                catch ( ResourceNotFoundException )
                {
                    RemoteInspectorServer.LogWarning( "Resource not found : " + path );
                }
            } );

            if ( content == null )
            {
                return false;
            }

            response.Send( content, contentType );
            return true;
        }

        private static string GuessContentTypeForPath( string path )
        {
            var contentType = MimeTypes.Text.Plain;

            if ( path.EndsWith( ".html" ) )
            {
                contentType = MimeTypes.Text.Html;
            }
            else if ( path.EndsWith( ".js" ) )
            {
                contentType = MimeTypes.Application.Javascript;
            }
            else if ( path.EndsWith( ".css" ) )
            {
                contentType = MimeTypes.Text.Css;
            }
            else if ( path.EndsWith( ".png" ) )
            {
                contentType = MimeTypes.Image.Png;
            }
            else if ( path.EndsWith( ".jpg" ) || path.EndsWith( ".jpeg" ) )
            {
                contentType = MimeTypes.Image.Jpeg;
            }
            else if ( path.EndsWith( ".ico" ) )
            {
                contentType = MimeTypes.Image.Icon;
            }

            return contentType;
        }
    }
}