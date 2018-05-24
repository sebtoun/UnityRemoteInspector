using System.Collections.Generic;
using RemoteInspector.Middlewares;
using RemoteInspector.Server;
using UnityEngine;

namespace RemoteInspector
{
    public class RemoteInspector : MonoBehaviour
    {
        public int ServerPort = 8080;

        private RemoteInspectorServer _server;

        private IViewEngine _viewEngine;

        private void InitServer()
        {
            _server = new RemoteInspectorServer( (ushort) ServerPort );

            _viewEngine = new MustachioViewEngine( "Views" );

            _server.Use( "/api/", new ApiServer() );

            _server.Use( "/", ( request, response, relativePath ) =>
            {
                const string indexViewPath = "index.html";

                if ( relativePath != "" )
                {
                    return false;
                }

                try
                {
                    var context = new Dictionary<string, object>()
                    {
                        { "content", "Hey!" }
                    };

                    var content = _viewEngine.Render( indexViewPath, context );
                    response.Send( content, MimeTypes.Text.Html );
                    return true;
                }
                catch ( ViewNotFoundException )
                {
                    RemoteInspectorServer.LogError( "Main interface view not found" );
                    return false;
                }
            } );

            _server.Use( "/", new StaticFileServer( "Assets" ) );
            
//            _server.Use( "/", ( request, response, relativePath ) =>
//            {
//                var url = request.Url.GetLeftPart( UriPartial.Authority ) + "/";
//                response.Redirect( url );
//                response.Close();
//                return true;
//            } );
        }

        private void OnEnable()
        {
            StartServer();
        }

        private void OnDisable()
        {
            StopServer();
        }

        private void StartServer()
        {
            if ( _server == null )
            {
                InitServer();
            }

            _server.Start();
        }

        private void StopServer()
        {
            _server.Stop();
        }

        private void OnValidate()
        {
            ServerPort = Mathf.Min( ushort.MaxValue, ServerPort );
            ServerPort = Mathf.Max( 0, ServerPort );
        }
    }
}