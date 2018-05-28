using System.Collections.Generic;
using RemoteInspector.Server;

namespace RemoteInspector.Middlewares
{
    public abstract class ViewEngineBase<TTemplate> : IViewEngine
    {
        private string _templatesRoot;

        private readonly bool _cacheSources;

        private readonly Dictionary<string, TTemplate> _cachedTemplates = new Dictionary<string, TTemplate>();

        protected ViewEngineBase( string rootPath, bool cacheSources = false )
        {
            _templatesRoot = rootPath;
            _cacheSources = cacheSources;
        }

        public virtual string Render( string path, IDictionary<string, object> context )
        {
            return ApplyTemplate( GetTemplate( path ), context );
        }

        protected abstract string ApplyTemplate( TTemplate template, IDictionary<string, object> context );

        protected abstract TTemplate CreateTemplate( string templateSource );

        protected TTemplate GetTemplate( string path )
        {
            TTemplate template;
            if ( !_cacheSources || !_cachedTemplates.TryGetValue( path, out template ) )
            {
                var sourceTemplate = GetSourceTemplate( path );

                template = CreateTemplate( sourceTemplate );

                if ( _cacheSources )
                {
                    _cachedTemplates.Add( path, template );
                }
            }

            return template;
        }

        protected string GetSourceTemplate( string path )
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