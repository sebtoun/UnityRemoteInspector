using System;
using UnityEditor;
using UnityEngine;

namespace RemoteInspector.Editor
{
    public class RemoteInspectorEditorWindow : EditorWindow
    {
        [ NonSerialized ]
        private RemoteInspectorServer _server;

        private bool _serverStarted;

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
            EditorGUILayout.LabelField( "Server " + ( IsServerRunning ? "is running" : "is not running" ) );
            if ( IsServerRunning )
            {
                if ( GUILayout.Button( "Stop Server" ) )
                {
                    _serverStarted = false;
                    StopServer();
                }
            }
            else
            {
                if ( GUILayout.Button( "Start Server" ) )
                {
                    _serverStarted = true;
                    StartServer();
                }
            }
        }

        private void OnEnable()
        {
            if ( _serverStarted )
            {
                StartServer();
            }
        }

        private void OnDisable()
        {
            if ( IsServerRunning )
            {
                StopServer();
            }            
        }

        private void StartServer()
        {
            if ( _server == null )
            {
                _server = new RemoteInspectorServer();
            }

            _server.Start();
        }

        private void StopServer()
        {
            _server.Stop();
        }
    }
}