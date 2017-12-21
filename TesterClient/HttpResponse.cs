using System;
using System.Net;
using TesterClient.Api;

namespace TesterClient
{
    public class HttpResponse
    {
        /// <summary>
        /// Status Code HTTP/1.1 ответа
        /// </summary>
        public HttpStatusCode StatusCode;
        public string RawData;
        /// <summary>
        /// Распарсить HTTP/1.1 ответ
        /// </summary>
        /// <param name="message">HTTP/1.1 ответ в чистом виде</param>
        public HttpResponse(string message)
        {
            
            // Определим место, где заканчиваются headers
            int posRnRn = message.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (posRnRn == -1)
                posRnRn = message.IndexOf("\n\n", StringComparison.Ordinal);
            RawData = message.Substring(posRnRn);
            string firstLine = message.Substring(0, message.IndexOf('\n'));
            try
            {
                StatusCode = (HttpStatusCode)Convert.ToInt32(firstLine.Split(' ')[1]);
            }
            catch
            {
                StatusCode = HttpStatusCode.BadRequest;
            }
            

        }
    }
}