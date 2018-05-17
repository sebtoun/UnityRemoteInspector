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

                response.Send( HttpStatusCode.NotImplemented );
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
            try
            {
                PrepareResponse( response, code );
            }
            finally
            {
                response.Close();
            }
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
            try
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
            }
            finally
            {
                response.Close();
            }
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

        public static void SendFile( this HttpListenerResponse response, string path, bool sendChunked = false )
        {
            var contentType = MimeTypes.Text.Plain;

            if ( path.EndsWith( ".html" ) )
            {
                contentType = MimeTypes.Text.Html;
            }
            else if ( path.EndsWith( ".js" ) )
            {
                contentType = MimeTypes.Application.Javascript;
            }
            else if ( path.EndsWith( ".css" ) )
            {
                contentType = MimeTypes.Text.Css;
            }
            else if ( path.EndsWith( ".png" ) )
            {
                contentType = MimeTypes.Image.Png;
            }
            else if ( path.EndsWith( ".jpg" ) || path.EndsWith( ".jpeg" ) )
            {
                contentType = MimeTypes.Image.Jpeg;
            }
            else if ( path.EndsWith( ".ico" ) )
            {
                contentType = MimeTypes.Image.Icon;
            }

            SendFile( response, path, contentType, sendChunked );
        }

        public static void SendFile( this HttpListenerResponse response, string path, string contentType,
            bool sendChunked = false )
        {
            if ( contentType.StartsWith( "text/" ) )
            {
                try
                {
                    var contentString = ResourceLoader.GetTextFileContent( path );
                    Send( response, contentString, contentType, sendChunked: sendChunked );
                }
                catch ( ResourceNotFoundException )
                {
                    Debug.Log( "Resource not found : " + path );
                    Send( response, HttpStatusCode.NotFound );
                }
            }
            else
            {
                try
                {
                    var contentBinary = ResourceLoader.GetBinaryFileContent( path );
                    Send( response, contentBinary, contentType, sendChunked: sendChunked );
                }
                catch ( ResourceNotFoundException )
                {
                    Debug.Log( "Resource not found : " + path );
                    Send( response, HttpStatusCode.NotFound );
                }
            }
        }
    }
}