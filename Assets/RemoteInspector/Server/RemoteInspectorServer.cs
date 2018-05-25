using System.Collections.Generic;
using RemoteInspector.Middlewares;

namespace RemoteInspector.Server
{
    public class RemoteInspectorServer : MiddlewareServer
    {
        public RemoteInspectorServer( ushort port = 8080 ) : base( port )
        {
            var viewEngine = new MustachioViewEngine( "Views" );

            Use( "/ping", ( request, response, path ) =>
            {
                if ( path != "" )
                {
                    return false;
                }

                response.Send( "pong" );
                return true;
            } );
            
            Use( "/api", new ApiServer() );

            Use( ( request, response, relativePath ) =>
            {
                const string indexViewPath = "index.html";

                if ( relativePath != "/" )
                {
                    return false;
                }

                try
                {
                    var context = new Dictionary<string, object>()
                    {
                        { "content", "Hey!" }
                    };

                    var content = viewEngine.Render( indexViewPath, context );
                    response.Send( content, MimeTypes.Text.Html );
                    return true;
                }
                catch ( ViewNotFoundException )
                {
                    LogError( "Main interface view not found" );
                    return false;
                }
            } );

            Use( new StaticFileServer( "Assets" ) );
        }
    }
}