using System.Collections.Generic;
using System.Linq;
using RemoteInspector.Middlewares;
using UnityEngine;

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

            Use<ApiServer>( "/api" );

            Use( ( request, response, relativePath ) =>
            {
                const string viewPath = "index.html";

                try
                {
                    Dictionary<string, object>[] childrens = null;
                    Dictionary<string, object>[] parents = null;
                    Dictionary<string, object>[] components = null;
                    bool found = false;

                    UnityMainThreadDispatcher.Instance().EnqueueAndWait( () =>
                    {
                        // remove leading '/'
                        relativePath = relativePath.Substring( 1 );

                        if ( string.IsNullOrEmpty( relativePath ) )
                        {
                            components = null;
                            parents = null;
                            found = true;
                        }
                        else
                        {
                            var gameObject = GameObject.Find( relativePath );
                            if ( gameObject == null )
                            {
                                found = false;
                            }
                            else
                            {
                                found = true;

                                parents = gameObject.transform.WalkUpwards().Reverse().Select( tr =>
                                {
                                    return new Dictionary<string, object>()
                                    {
                                        { "id", tr.gameObject.GetInstanceID() },
                                        { "name", tr.name },
                                        { "path", tr.FullPath() }
                                    };
                                } ).ToArray();

                                components = gameObject.GetComponents<UnityEngine.Component>().Select( comp =>
                                {
                                    return new Dictionary<string, object>()
                                    {
                                        { "id", comp.GetInstanceID() },
                                        { "name", comp.GetType().Name }
                                    };
                                } ).ToArray();
                            }
                        }

                        if ( found )
                        {
                            childrens = SceneExplorer.ListChildrens( relativePath ).Select( go =>
                            {
                                return new Dictionary<string, object>()
                                {
                                    { "id", go.GetInstanceID() },
                                    { "name", go.name },
                                    { "path", go.transform.FullPath() }
                                };
                            } ).ToArray();
                        }
                    } );

                    if ( !found )
                    {
                        return false;
                    }

                    var context = new Dictionary<string, object>()
                    {
                        { "parents", parents },
                        { "childrens", childrens },
                        { "components", components }
                    };

                    var content = viewEngine.Render( viewPath, context );
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