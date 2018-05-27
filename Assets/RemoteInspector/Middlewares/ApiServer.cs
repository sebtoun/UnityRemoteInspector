using RemoteInspector.Server;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace RemoteInspector.Middlewares
{
    public class ApiServer : WebSocketBehavior
    {
        protected override void OnMessage( MessageEventArgs e )
        {
            base.OnMessage( e );
            MiddlewareServer.Log( e.Data );
            Send( e.Data );
        }
    }
}