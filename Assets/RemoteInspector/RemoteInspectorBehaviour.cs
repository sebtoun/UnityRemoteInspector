using System.Collections.Generic;
using RemoteInspector.Middlewares;
using RemoteInspector.Server;
using UnityEngine;

namespace RemoteInspector
{
    public class RemoteInspectorBehaviour : MonoBehaviour
    {
        public int ServerPort = 8080;

        private MiddlewareServer _server;

        private void InitServer()
        {
            _server = new RemoteInspectorServer( (ushort) ServerPort );
        }

        private void OnEnable()
        {
            StartServer();
        }

        private void OnDisable()
        {
            StopServer();
        }

        private void StartServer()
        {
            if ( _server == null )
            {
                InitServer();
            }

            _server.Start();
        }

        private void StopServer()
        {
            _server.Stop();
        }

        private void OnValidate()
        {
            ServerPort = Mathf.Min( ushort.MaxValue, ServerPort );
            ServerPort = Mathf.Max( 0, ServerPort );
        }
    }
}