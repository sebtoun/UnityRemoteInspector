using System;
using System.Collections.Generic;

namespace RemoteInspector.Server
{
    public interface IViewEngine
    {
        string Render( string path, IDictionary<string, object> context );
    }


    public class ViewNotFoundException : Exception
    {
        public ViewNotFoundException( string path ) : base( "Specified view is not found: " + path )
        {
        }
    }
}