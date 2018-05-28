using System.Collections.Generic;
using System.IO;
using Nustache.Core;

namespace RemoteInspector.Middlewares
{
    public class NustacheViewEngine : ViewEngineBase<Template>
    {
        public NustacheViewEngine( string rootPath, bool cacheSources = false ) : base( rootPath, cacheSources )
        {
        }

        protected override string ApplyTemplate( Template template, IDictionary<string, object> context )
        {
            var output = new StringWriter();
            template.Render( context, output, GetTemplate );
            return output.ToString();
        }

        protected override Template CreateTemplate( string templateSource )
        {
            var template = new Template();
            template.Load( new StringReader( templateSource ) );
            return template;
        }
    }
}