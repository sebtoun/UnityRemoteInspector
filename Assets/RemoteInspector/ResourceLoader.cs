using System;
using UnityEngine;

namespace RemoteInspector
{
    public static class ResourceLoader
    {
        public static byte[] GetBinaryFileContent( string resourcePath )
        {
            var resource = Resources.Load<TextAsset>( resourcePath );
            if ( resource == null )
            {
                throw new ResourceNotFoundException( resourcePath );
            }

            return resource.bytes;
        }

        public static string GetTextFileContent( string resourcePath )
        {
            var resource = Resources.Load<TextAsset>( resourcePath );
            if ( resource == null )
            {
                throw new ResourceNotFoundException( resourcePath );
            }

            return resource.text;
        }
    }

    public class ResourceNotFoundException : ApplicationException
    {
        public ResourceNotFoundException( string path ) : base( "Specified resource is not found: " + path )
        {
        }
    }
}