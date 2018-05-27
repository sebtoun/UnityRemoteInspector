using System;
using System.Collections.Generic;
using RemoteInspector.Server;

namespace RemoteInspector.Middlewares
{
    public class MustachioViewEngine : IViewEngine
    {
        private readonly string _templatesRoot;
        private bool _cacheSources;

        private readonly Dictionary<string, Func<IDictionary<string, object>, string>> _cachedTemplates =
            new Dictionary<string, Func<IDictionary<string, object>, string>>();

        public MustachioViewEngine( string rootPath, bool cacheSources = false )
        {
            _templatesRoot = rootPath;
            _cacheSources = cacheSources;
        }

        public string Render( string path, IDictionary<string, object> context )
        {
            Func<IDictionary<string, object>, string> template;
            if ( !_cacheSources || !_cachedTemplates.TryGetValue( path, out template ) )
            {
                var sourceTemplate = GetSourceTemplate( path );

                template = Mustachio.Parser.Parse( sourceTemplate );
                
                if ( _cacheSources )
                {
                    _cachedTemplates.Add( path, template );
                }
            }

            return template( context );
        }

        private string GetSourceTemplate( string path )
        {
            path = _templatesRoot + "/" + path;

            string sourceTemplate = null;
            UnityMainThreadDispatcher.Instance().EnqueueAndWait( () =>
            {
                try
                {
                    sourceTemplate = ResourceLoader.GetTextFileContent( path );
                }
                catch ( ResourceNotFoundException )
                {
                    MiddlewareServer.LogWarning( "View not found : " + path );
                }
            } );

            if ( sourceTemplate == null )
            {
                throw new ViewNotFoundException( path );
            }

            return sourceTemplate;
        }
    }
}