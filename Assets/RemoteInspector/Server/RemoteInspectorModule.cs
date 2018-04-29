namespace RemoteInspector.Server
{
    public class RemoteInspectorModule : Nancy.NancyModule
    {
        public RemoteInspectorModule()
        {
            Get["/"] = _ => "Hello World!";
        }
    }
}