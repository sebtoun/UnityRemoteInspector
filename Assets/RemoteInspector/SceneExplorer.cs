using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RemoteInspector.Server;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RemoteInspector
{
    public static class SceneExplorer
    {
        public static IEnumerable<GameObject> ListChildren( string path = null )
        {
            if ( string.IsNullOrEmpty( path ) )
            {
                foreach ( var rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects()
                    .Union( UnityMainThreadDispatcher.Instance().gameObject.scene.GetRootGameObjects() ) )
                {
                    yield return rootGameObject;
                }
            }
            else
            {
                var root = GameObject.Find( path );
                if ( root == null )
                {
                    throw new ObjectNotFoundException( path );
                }

                foreach ( Transform child in root.transform )
                {
                    yield return child.gameObject;
                }
            }
        }

        public static Dictionary<string, object> InspectRoot()
        {
            var content = new Dictionary<string, object>();

            content[ "name" ] = "_root_";
            content[ "id" ] = 0;
            content[ "path" ] = "";

            content[ "childrens" ] = ListChildren().Select( go =>
            {
                return new Dictionary<string, object>()
                {
                    { "id", go.GetInstanceID() },
                    { "name", go.name },
                    { "path", go.transform.FullPath() }
                };
            } ).ToArray();

            return content;
        }

        public static Dictionary<string, object> CreateRemoteView( Component component )
        {
            var result = new Dictionary<string, object>();
            
            result[ "id" ] = component.GetInstanceID();
            result[ "name" ] = component.name;
            result[ "path" ] = component.transform.FullPath();
            List<Dictionary<string, object>> properties;
            result["properties"] = properties = new List<Dictionary<string, object>>();

            foreach ( var fieldInfo in component.GetType()
                .GetFields( BindingFlags.Instance |
                            BindingFlags.NonPublic |
                            BindingFlags.Public ) )
            {
                if ( fieldInfo.FieldType.IsSerializable &&
                     ( fieldInfo.IsPublic &&
                       fieldInfo.GetCustomAttributes( typeof( HideInInspector ), false ).Length == 0
                       || !fieldInfo.IsPublic &&
                       fieldInfo.GetCustomAttributes( typeof( SerializeField ), false ).Length > 0 ) )
                {
                    try
                    {
                        properties.Add( new Dictionary<string, object>()
                        {
                            { "name", fieldInfo.Name },
                            { "type", fieldInfo.FieldType.FullName },
                            { "value", fieldInfo.GetValue( component ) }
                        } );
                    }
                    catch ( TargetInvocationException )
                    {
                    }
                }
            }

            foreach ( var propInfo in component.GetType()
                .GetProperties( BindingFlags.Instance |
                                BindingFlags.Public ) )
            {
                if ( propInfo.PropertyType.IsValueType && propInfo.CanRead && propInfo.GetGetMethod() != null )
                {
                    try
                    {
                        properties.Add( new Dictionary<string, object>()
                        {
                            { "name", propInfo.Name },
                            { "type", propInfo.PropertyType.FullName },
                            { "value", propInfo.GetGetMethod().Invoke( component, null ) }
                        } );
                    }
                    catch ( TargetInvocationException e )
                    {
                        MiddlewareServer.LogException( e );
                    }
                }
            }

            return result;
        }

        public static Dictionary<string, object> InspectGameObject( GameObject gameObject )
        {
            var content = new Dictionary<string, object>();
            var objectPath = gameObject.transform.FullPath();

            content[ "name" ] = gameObject.name;
            content[ "id" ] = gameObject.GetInstanceID();
            content[ "path" ] = objectPath;

            content[ "components" ] = gameObject.GetComponents<Component>().Select( comp =>
            {
                var id = comp.GetInstanceID();
                return new Dictionary<string, object>()
                {
                    { "id", id },
                    { "name", comp.GetType().Name },
                    { "path", objectPath + "?id=" + id }
                };
            } ).ToArray();

            content[ "parents" ] = gameObject.transform.WalkUpwards().Reverse().Select( tr =>
            {
                return new Dictionary<string, object>()
                {
                    { "id", tr.gameObject.GetInstanceID() },
                    { "name", tr.name },
                    { "path", tr.FullPath() }
                };
            } ).ToArray();

            content[ "childrens" ] = ListChildren( objectPath ).Select( go =>
            {
                return new Dictionary<string, object>()
                {
                    { "id", go.GetInstanceID() },
                    { "name", go.name },
                    { "path", go.transform.FullPath() }
                };
            } ).ToArray();

            return content;
        }
    }

    public class ObjectNotFoundException : ApplicationException
    {
        public ObjectNotFoundException( string path ) : base( "Specified object is not found: " + path )
        {
        }
    }
}