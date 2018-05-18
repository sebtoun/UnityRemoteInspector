using RemoteInspector.Server;
using UnityEngine;

namespace RemoteInspector
{
    public class RemoteInspector : MonoBehaviour
    {
        public int ServerPort = 8080;

        private RemoteInspectorServer _server;

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
                _server = new RemoteInspectorServer( (ushort) ServerPort );
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