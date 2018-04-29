using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using Nancy.TinyIoc;

namespace RemoteInspector.Server
{
    public class SimpleNancyBootstrapper : DefaultNancyBootstrapper
    {
        protected override IEnumerable<Func<Assembly, bool>> AutoRegisterIgnoredAssemblies
        {
            get
            {
                return base.AutoRegisterIgnoredAssemblies.Union( new Func<Assembly, bool>[]
                {
                    (Func<Assembly, bool>) ( asm => asm.FullName.StartsWith( "Mono.", StringComparison.Ordinal ) ),
                    (Func<Assembly, bool>) ( asm => asm.FullName.StartsWith( "Boo.", StringComparison.Ordinal ) ),
                    (Func<Assembly, bool>) ( asm => asm.FullName.StartsWith( "Boo.", StringComparison.Ordinal ) ),
                    (Func<Assembly, bool>) ( asm => asm.FullName.StartsWith( "Unity.", StringComparison.Ordinal ) ),
                    (Func<Assembly, bool>) ( asm =>
                        asm.FullName.StartsWith( "UnityEditor.", StringComparison.Ordinal ) ),
                    (Func<Assembly, bool>) ( asm =>
                        asm.FullName.StartsWith( "UnityEditor,", StringComparison.Ordinal ) ),
                    (Func<Assembly, bool>) ( asm =>
                        asm.FullName.StartsWith( "UnityEngine.", StringComparison.Ordinal ) ),
                    (Func<Assembly, bool>) ( asm =>
                        asm.FullName.StartsWith( "UnityEngine,", StringComparison.Ordinal ) ),
                    (Func<Assembly, bool>) ( asm => asm.FullName.StartsWith( "ExCSS.", StringComparison.Ordinal ) ),
                    (Func<Assembly, bool>) ( asm =>
                        asm.FullName.StartsWith( "UnityScript,", StringComparison.Ordinal ) ),
                    (Func<Assembly, bool>) ( asm =>
                        asm.FullName.StartsWith( "WindowsBase,", StringComparison.Ordinal ) ),
                    (Func<Assembly, bool>) ( asm => asm.FullName.StartsWith( "nunit.", StringComparison.Ordinal ) ),
                    (Func<Assembly, bool>) ( asm => asm.FullName.StartsWith( "JetBrains.", StringComparison.Ordinal ) )
                } );
            }
        }

//        protected override DiagnosticsConfiguration DiagnosticsConfiguration
//        {
//            get
//            {
//                var conf = base.DiagnosticsConfiguration;
//                conf.Password = "123";
//                return conf;
//            }
//        }

        protected override void ApplicationStartup( TinyIoCContainer container, IPipelines pipelines )
        {
            DiagnosticsHook.Disable( pipelines );
            base.ApplicationStartup( container, pipelines );
        }
    }
}