using System;
using System.Text;
using Newtonsoft.Json;
using WebSocketSharp.Net;

namespace RemoteInspector.Server
{
    public static class RequestsExtensions
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

        public static void Send( this HttpListenerResponse response, string content,
            string contentType = MimeTypes.Text.Plain, HttpStatusCode code = HttpStatusCode.OK,
            bool sendChunked = false )
        {
            var data = Encoding.UTF8.GetBytes( content );

            Send( response, data, contentType, code, sendChunked );
        }

        public static void Send( this HttpListenerResponse response, byte[] content,
            string contentType = MimeTypes.Application.OctetStream, HttpStatusCode code = HttpStatusCode.OK,
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

//        public static void Redirect( this HttpListenerResponse response, Uri url )
//        {
//            Redirect( response, url.ToString() );
//        }
//
//        public static void Redirect( this HttpListenerResponse response, string path )
//        {
//            try
//            {
//                PrepareResponse( response, HttpStatusCode.Found );
//                response.AddHeader( "Location", path );
//            }
//            finally
//            {
//                response.Close();
//            }
//        }
    }
}