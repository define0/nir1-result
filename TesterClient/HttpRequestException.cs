using System;
using System.Web;

namespace TesterClient
{
    public class HttpRequestException : Exception
    {
        public HttpRequestException(string message):base(message) 
        {
        }
    }
}