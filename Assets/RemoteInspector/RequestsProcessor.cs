using System;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using WebSocketSharp.Net;

namespace RemoteInspector
{
    public class RequestsProcessor
    {
        public void ProcessRequest( HttpListenerRequest request, HttpListenerResponse response )
        {
            var method = request.HttpMethod;
            var path = request.Url.AbsolutePath;

            try
            {
                if ( path.StartsWith( "/api/" ) )
                {
                    ProcessApiCall( request, response );
                    return;
                }

                if ( method == "GET" )
                {
                    path = "Assets" + path;
                    UnityMainThreadDispatcher.Instance().Enqueue( () => response.SendFile( path ) );
                    return;
                }

                response.Send( HttpStatusCode.NotFound );
            }
            catch ( Exception exception )
            {
                response.SendException( exception );
                throw;
            }
        }

        private void ProcessApiCall( HttpListenerRequest request, HttpListenerResponse response )
        {
            response.Send( HttpStatusCode.NotImplemented );
        }
    }

    public static class Extensions
    {
        private static void PrepareResponse( HttpListenerResponse response, HttpStatusCode code = HttpStatusCode.OK,
            string contentType = null, bool sendChunked = false, long contentLength = 0 )
        {
            response.StatusCode = (int) code;

            if ( contentType != null )
            {
                response.ContentType = contentType;

                if ( sendChunked )
                {
                    response.SendChunked = true;
                }
                else
                {
                    response.ContentLength64 = contentLength;
                }
            }
        }

        public static void Send( this HttpListenerResponse response, HttpStatusCode code = HttpStatusCode.OK )
        {
            PrepareResponse( response, code );
            response.Close();
        }

        public static void Send( this HttpListenerResponse response,
            string content, string contentType = MimeTypes.Text.Plain, HttpStatusCode code = HttpStatusCode.OK,
            bool sendChunked = false )
        {
            var data = Encoding.UTF8.GetBytes( content );

            Send( response, data, contentType, code, sendChunked );
        }

        public static void Send( this HttpListenerResponse response,
            byte[] content, string contentType = MimeTypes.Application.OctetStream,
            HttpStatusCode code = HttpStatusCode.OK,
            bool sendChunked = false )
        {
            if ( sendChunked )
            {
                PrepareResponse( response, code, contentType, true );
            }
            else
            {
                PrepareResponse( response, code, contentType, false, content.LongLength );
            }

            response.OutputStream.Write( content, 0, content.Length );
            response.Close();
        }

        public static void SendJson( this HttpListenerResponse response, object obj )
        {
            Send( response, content: JsonConvert.SerializeObject( obj ), contentType: MimeTypes.Application.Json );
        }

        public static void SendException( this HttpListenerResponse response, Exception exception )
        {
            Send( response, content: exception.Message + "\n" + exception.StackTrace,
                code: HttpStatusCode.InternalServerError );
        }

        public static void SendFile( this HttpListenerResponse response, string path,
            string contentType = MimeTypes.Text.Plain, bool sendChunked = false )
        {
            Debug.Log( "loading file " + path );
            if ( contentType.StartsWith( "text/" ) )
            {
                string contentString = null;
                try
                {
                    contentString = ResourceLoader.GetTextFileContent( path );
                }
                catch ( ResourceNotFoundException )
                {
                    Debug.Log(  );
                    Send( response, HttpStatusCode.NotFound );
                }

                Send( response, contentString, contentType, sendChunked: sendChunked );
            }
            else
            {
                byte[] contentBinary = null;
                try
                {
                    contentBinary = ResourceLoader.GetBinaryFileContent( path );
                }
                catch ( ResourceNotFoundException )
                {
                    Send( response, HttpStatusCode.NotFound );
                }

                Send( response, contentBinary, contentType, sendChunked: sendChunked );
            }
        }
    }
}