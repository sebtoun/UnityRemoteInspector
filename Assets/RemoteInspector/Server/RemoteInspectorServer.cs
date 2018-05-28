using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RemoteInspector.Middlewares;
using UnityEngine;
using UnityEngine.SceneManagement;
using HttpListenerRequest = WebSocketSharp.Net.HttpListenerRequest;
using HttpListenerResponse = WebSocketSharp.Net.HttpListenerResponse;

namespace RemoteInspector.Server
{
    public class RemoteInspectorServer : MiddlewareServer
    {
        public RemoteInspectorServer( ushort port = 8080 ) : base( port )
        {
            var viewEngine = new NustacheViewEngine( "Views" );

            Use( "/ping", ( request, response, path ) =>
            {
                if ( path != "" )
                {
                    return false;
                }

                response.Send( "pong" );
                return true;
            } );

            Use( "/api/go", new GameObjectExplorerService() );

            Use( ( request, response, relativePath ) =>
            {
                Dictionary<string, object> content = null;
                var found = false;
                Exception error = null;

                UnityMainThreadDispatcher.Instance().EnqueueAndWait( () =>
                {
                    try
                    {
                        // remove leading '/'
                        relativePath = relativePath.Substring( 1 );

                        if ( string.IsNullOrEmpty( relativePath ) )
                        {
                            content = SceneExplorer.InspectRoot();
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
                                content = SceneExplorer.InspectGameObject( gameObject );

                                var idString = request.QueryString.Get( "id" );
                                var componentId = 0;

                                var findTargetId = !string.IsNullOrEmpty( idString ) &&
                                                   int.TryParse( idString, out componentId );

                                if ( findTargetId )
                                {
                                    content[ "componentDetails" ] = SceneExplorer.CreateRemoteView( gameObject
                                        .GetComponents<Component>()
                                        .First( comp => comp.GetInstanceID() == componentId ) );
                                }
                            }
                        }
                    }
                    catch ( Exception exception )
                    {
                        error = exception;
                    }
                } );

                if ( error != null )
                {
                    LogException( error );
                    response.SendException( error );
                    return true;
                }

                if ( !found )
                {
                    return false;
                }

                try
                {
                    const string viewPath = "index.html";
                    var contentJson = JsonConvert.SerializeObject( content, Formatting.Indented, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        ContractResolver = new IgnorePropertiesContractResolver()
                    } );

                    var renderContext = JsonHelper.Deserialize( contentJson ) as Dictionary<string, object>;                    
                    var responseContent = viewEngine.Render( viewPath, content );

                    response.Send( responseContent, MimeTypes.Text.Html );
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

        private class GameObjectExplorerService : IRequestHandler
        {
            public bool HandleRequest( HttpListenerRequest request, HttpListenerResponse response, string relativePath )
            {
                var objectPath = relativePath.Substring( 1 );

                Dictionary<string, object> content = null;
                Exception error = null;
                var failContent = new Dictionary<string, object>();

                UnityMainThreadDispatcher.Instance().EnqueueAndWait( () =>
                {
                    try
                    {
                        if ( objectPath == "" )
                        {
                            content = SceneExplorer.InspectRoot();
                        }
                        else
                        {
                            var gameObject = GameObject.Find( objectPath );
                            if ( gameObject == null )
                            {
                                error = new ObjectNotFoundException( objectPath );
                                return;
                            }

                            content = SceneExplorer.InspectGameObject( gameObject );

                            var idString = request.QueryString.Get( "id" );
                            var componentId = 0;

                            var findTargetId = !string.IsNullOrEmpty( idString ) &&
                                               int.TryParse( idString, out componentId );

                            if ( findTargetId )
                            {
                                content[ "component" ] = SceneExplorer.CreateRemoteView( gameObject
                                    .GetComponents<Component>()
                                    .First( comp => comp.GetInstanceID() == componentId ) );
                            }
                        }
                    }
                    catch ( Exception exception )
                    {
                        error = exception;
                    }
                } );

                if ( error != null )
                {
                    response.SendJson( JsonReponse.Error( error.Message ) );
                }
                else if ( failContent.Count > 0 )
                {
                    response.SendJson( JsonReponse.Fail( failContent ) );
                }
                else
                {
                    response.SendJson( JsonReponse.Success( content ) );
                }

                return true;
            }
        }
    }

    public struct JsonReponse
    {
        [ JsonConverter( typeof( StringEnumConverter ) ) ]
        public enum Status
        {
            success,
            fail,
            error
        }

        public Status status;
        public Dictionary<string, object> data;
        public string message;
        public int? code;

        public static JsonReponse Success( Dictionary<string, object> data )
        {
            return new JsonReponse()
            {
                status = Status.success,
                data = data
            };
        }

        public static JsonReponse Fail( Dictionary<string, object> data )
        {
            return new JsonReponse()
            {
                status = Status.fail,
                data = data
            };
        }

        public static JsonReponse Error( string message, int? code = null )
        {
            return new JsonReponse()
            {
                status = Status.error,
                message = message,
                code = code
            };
        }
    }
}