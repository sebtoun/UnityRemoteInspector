using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RemoteInspector
{
    public static class SceneExplorer
    {
        public static IEnumerable<GameObject> ListChildrens( string path = null )
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
        
        
    }

    public class ObjectNotFoundException : ApplicationException
    {
        public ObjectNotFoundException( string path ) : base( "Specified object is not found: " + path )
        {
        }
    }
}