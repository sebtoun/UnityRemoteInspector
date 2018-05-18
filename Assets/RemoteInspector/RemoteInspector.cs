using UnityEngine;

namespace RemoteInspector
{
    public class RemoteInspector : MonoBehaviour
    {
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