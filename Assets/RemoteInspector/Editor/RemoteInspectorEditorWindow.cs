using System;
using System.Security.Cryptography;
using UnityEditor;

namespace RemoteInspector.Editor
{
    public class RemoteInspectorEditorWindow : EditorWindow
    {
        [ NonSerialized ]
        private RemoteInspectorServer _server;

        private bool IsServerRunning
        {
            get { return _server != null && _server.IsRunning; }
        }
        
        [ MenuItem( "Window/Remote Inspector" ) ]
        private static void Init()
        {
            GetWindow<RemoteInspectorEditorWindow>();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField( "Remote Inspector " + ( IsServerRunning ? "is running" : "is not running" ) );
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
            if ( IsServerRunning )
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
            InitializeServer();
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

        private void InitializeServer()
        {
            _server = new RemoteInspectorServer();
        }        

        private void StartServer()
        {
            if ( _server == null )
            {
                InitializeServer();
            }

            _server.Start();
        }

        private void StopServer()
        {
            _server.Stop();
        }
    }
}