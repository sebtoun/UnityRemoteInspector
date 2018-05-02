using System;
using Nancy.Hosting.Self;
using RemoteInspector.Server;
using UnityEditor;
using UnityEngine;

namespace RemoteInspector.Editor.Server
{
    public class RemoteInspectorServer : EditorWindow
    {
        private const string LogPrefix = "[RemoteInspector] ";
        private const string Uri = "http://localhost:8080";

        [ NonSerialized ]
        private NancyHost _nancyHost;

        private bool IsRunning
        {
            get { return _nancyHost != null; }
        }

        [ MenuItem( "Window/Remote Inspector" ) ]
        private static void Init()
        {
            GetWindow<RemoteInspectorServer>();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField( "Remote Inspector " + ( IsRunning ? "is running" : "is not running" ) );
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            if ( IsRunning )
            {
                StopServer();
            }
        }

        private void OnPlayModeStateChanged( PlayModeStateChange state )
        {
            switch ( state )
            {
                case PlayModeStateChange.EnteredPlayMode:
                    StartServer();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    StopServer();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.ExitingEditMode:
                default:
                    // do nothing
                    break;
            }
        }

        private void OnAfterAssemblyReload()
        {
            if ( EditorApplication.isPlaying )
            {
                StartServer();
            }
        }

        private void OnBeforeAssemblyReload()
        {
            if ( EditorApplication.isPlaying )
            {
                StopServer();
            }
        }

        private void StartServer()
        {
            if ( IsRunning )
            {
                LogError( "Cannot start Remote Inspector host since it is already running" );
                return;
            }

            try
            {
                var hostConfigs = new HostConfiguration
                {
                    UnhandledExceptionCallback = LogException
                };

                Log( "Starting Remote Inspector host" );
                _nancyHost = new NancyHost( new SimpleNancyBootstrapper(), hostConfigs, new Uri( Uri ) );
                _nancyHost.Start();
                Log( "Remote Inspector host started" );
            }
            catch ( Exception e )
            {
                LogError( "An exception occured during Remote Inspector server setup." );
                LogException( e );
                _nancyHost = null;
            }
        }

        private void StopServer()
        {
            if ( !IsRunning )
            {
                LogError( "Cannot stop Remote Inspector host since it is not running" );
                return;
            }

            try
            {
                Log( "Stopping Remote Inspector host" );
                _nancyHost.Stop();
                Log( "Remote Inspector server properly shut down" );
            }
            catch ( Exception e )
            {
                LogError( "An exception occured during Remote Inspector server stop." );
                LogException( e );
            }

            _nancyHost = null;
        }

        private static void Log( string message )
        {
            Debug.Log( LogPrefix + message );
        }

        private static void LogError( string message )
        {
            Debug.LogError( LogPrefix + message );
        }

        private static void LogException( Exception e )
        {
            var disposeExc = e as ObjectDisposedException;
            if ( disposeExc != null && disposeExc.ObjectName == "listener" )
            {
                // happens when stopping unity editor play mode
                return;
            }

            Debug.LogException( e );
        }
    }
}