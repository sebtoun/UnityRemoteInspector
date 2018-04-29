using System;
using System.Threading;
using Nancy.Hosting.Self;
using RemoteInspector.Server;
using UnityEngine;

public class RemoteInspectorServer : MonoBehaviour
{
    public bool UseSeparateThread = true;

    private const string Uri = "http://localhost:8080";
    
    private NancyHost _nancyHost;
    private Thread _serverThread;

    private string Timing
    {
        get { return DateTime.Now.ToString( "mm:ss.fff" ); }
    }
    
    private void Start()
    {
        StartServer();
    }

    private void OnDestroy()
    {
        StopServer();
    }

    private void StopServer()
    {
        if ( UseSeparateThread && _serverThread == null )
        {
            Debug.LogError( "Remote Inspector server is not running." );
            return;
        }

        if ( _nancyHost != null )
        {
            Debug.Log( "Stopping Remote Inspector host" );
            _nancyHost.Stop();
            _nancyHost = null;
            Debug.Log( "Remote Inspector server properly shut down" );
        }

        if ( UseSeparateThread )
        {
            Debug.Log( "Waiting Remote Inspector server thread" );
            _serverThread.Join();
            _serverThread = null;
        }
    }

    private void StartServer()
    {
        Debug.Log( Timing + ": Starting Remote Inspector" );
        
        if ( UseSeparateThread && _serverThread != null )
        {
            Debug.LogError( "Remote Inspector server already running." );
            return;
        }

        if ( UseSeparateThread )
        {
            _serverThread = new Thread( RunServer );
            _serverThread.Name = "Remote Inspector server thread";

            Debug.Log( "Starting Remote Inspector server thread" );
            _serverThread.Start();
        }
        else
        {
            RunServer();
        }
    }

    private void RunServer()
    {
        try
        {
            Debug.Log( Timing + ": Creating Remote Inspector host config" );
            var hostConfigs = new HostConfiguration
            {
                UnhandledExceptionCallback = Debug.LogException
            };

            Debug.Log( Timing + ": Creating Remote Inspector host" );
            _nancyHost = new NancyHost( new SimpleNancyBootstrapper(), hostConfigs, new Uri( Uri ) );

            Debug.Log( Timing + ": Starting Remote Inspector host" );
            _nancyHost.Start();
            
            Debug.Log( Timing + ": Remote Inspector host started" );
        }
        catch ( Exception e )
        {
            Debug.LogError( "An exception occured during Remote Inspector server setup." );
            Debug.LogException( e );
            _serverThread = null;
            _nancyHost = null;
        }
    }
}