using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System;
using System.CodeDom;
using System.Globalization;
using System.Text;
using TesterClient.Api;

namespace TesterClient
{
    public class HttpRequest
    {
        /// <summary>
        /// Метод HTTP/1.1
        /// </summary>
        public enum Method
        {
            GET,
            POST
        }

        /// <summary>
        /// Список со всеми headers HTTP/1.1 запроса
        /// </summary>
        private List<string> headersList = new List<string>();

        /// <summary>
        /// Флаг отладочного режима
        /// </summary>
        public bool Debug = false;

        /// <summary>
        /// Флаг Transfer-Encoding: chunked
        /// </summary>
        private readonly bool _chunked = false;

        /// <summary>
        /// Флаг окончания передачи данных на сервер
        /// </summary>
        private bool _end = false;

        /// <summary>
        /// Основной сокет передачи данных
        /// </summary>
        private Socket _socket = null;

        /// <summary>
        /// Хост, к которому подключается HttpRequest
        /// </summary>
        private readonly string _host;

        /// <summary>
        /// Порт, к которому подключается HttpRequest
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// HTTP/1.1 метод запроса
        /// </summary>
        private Method _method;

        /// <summary>
        /// Путь на сервере, к которому происходит запрос (HTTP/1.1)
        /// </summary>
        private string _path;

        /// <summary>
        /// HTTP/1.1 headers, готовые к отправке
        /// </summary>
        private string _rawHeaders;

        /// <summary>
        /// Данные, которые передаем на сервер
        /// </summary>
        private string _rawRequestData;

        /// <summary>
        /// Оставшиеся данные, которые нужно передать на сервер чанками
        /// </summary>
        private string _rawRequestDataTemp;

        /// <summary>
        /// Код ответа после запроса
        /// </summary>
        public HttpStatusCode StatusCode = 0;

        /// <summary>
        /// Content-Length запроса
        /// </summary>
        public int ContentLength
        {
            get { return _rawRequestData.Length; }
        }

        /// <summary>
        /// Текущая позиция отправки данных при chunked
        /// </summary>
        public int ChunkedPosition
        {
            get { return _rawRequestDataOffset; }
        }

        /// <summary>
        /// Текущая позиция отправки данных при chunked
        /// </summary>
        private int _rawRequestDataOffset = 0;

        /// <summary>
        /// Content-Type запроса
        /// </summary>
        private string _contentType;

        /// <summary>
        /// Keep-Alive запроса
        /// </summary>
        public int KeepAlive = 60;

        /// <summary>
        /// Конечная точка подключения (комбинация host:port)
        /// </summary>
        private IPEndPoint remoteEndPoint;

        /// <summary>
        /// User-Agent запроса
        /// </summary>
        public string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:57.0) Gecko/20100101 Firefox/57.0";

        /// <summary>
        /// Первичная инициализация запроса
        /// </summary>
        /// <param name="host">Хост</param>
        /// <param name="port">Порт</param>
        /// <param name="chunked">Является передача chunked</param>
        public HttpRequest(string host, int port, bool chunked = false)
        {
            _host = host;
            _port = port;
            _chunked = chunked;
            IPHostEntry ipHostInfo = Dns.GetHostEntry(_host);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            remoteEndPoint = new IPEndPoint(ipAddress, _port);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = 3000,
            };
        }

        /// <summary>
        /// Метод подготовки headers для запроса
        /// </summary>
        private void _prepareHeaders()
        {
            headersList.Add(string.Format("{0} {1} HTTP/1.1",
                _method.ToString().ToUpper(CultureInfo.CurrentCulture),
                _path));
            headersList.Add(string.Format("Host: {0}:{1}", _host, _port.ToString()));
            if (_chunked)
                headersList.Add("Transfer-Encoding: chunked");
            else
                headersList.Add("Content-Length: " + _rawRequestData.Length.ToString());
            headersList.Add("Keep-Alive: " + KeepAlive.ToString());
            headersList.Add("User-Agent: " + UserAgent);
            headersList.Add("Content-Type: " + _contentType);

            headersList.Add("MEPHI_SUPER_SECURE: SUPER_SECRET_PASS");
            _rawHeaders = string.Empty;
            foreach (string s in headersList)
                _rawHeaders += s + "\r\n";
            _rawHeaders += "\r\n";
        }

        /// <summary>
        /// Инициализация потока передаваемых данных и HTTP/1.1 методом GET
        /// </summary>
        /// <param name="path">HTTP/1.1 путь на сервере</param>
        /// <param name="request">Данные, передаваемые на сервер</param>
        public void Get(string path, IApiRequest request)
        {
            _method = Method.GET;
            _path = path;
            _rawRequestData = request.Raw();
            _contentType = request.ContentType();
            _prepareHeaders();
        }

        /// <summary>
        /// Инициализация потока передаваемых данных и HTTP/1.1 методом POST
        /// </summary>
        /// <param name="path">HTTP/1.1 путь на сервере</param>
        /// <param name="request">Данные, передаваемые на сервер</param>
        public void Post(string path, IApiRequest request)
        {
            _method = Method.POST;
            _path = path;
            _rawRequestData = request.Raw();
            _contentType = request.ContentType();
            _prepareHeaders();
        }

        /// <summary>
        /// Послать отдельно Headers на сервер
        /// </summary>
        public void SendHeader()
        {
            _socket.Connect(remoteEndPoint);
            byte[] buffer = Encoding.ASCII.GetBytes(_rawHeaders);
            if (Debug)
                Console.Write(_rawHeaders);
            _socket.Send(buffer);
            _rawRequestDataTemp = string.Copy(_rawRequestData);
        }

        /// <summary>
        /// Совершить передачу одного chunk. Не забудьте SendHeaders перед этим.
        /// </summary>
        /// <param name="count">Количество байт, передаваемых за чанк</param>
        /// <returns>true, если метод не подходит (включен chunked=false режим), или все данные переданы</returns>
        public bool Send(int count)
        {
            if (_chunked == false)
                return true;
            if (_end)
                return true;
            if (_rawRequestDataTemp.Length < count)
                count = _rawRequestDataTemp.Length;
            if (count == 0)
            {
                string sEndBuffer = "0\r\n\r\n";
                byte[] endBuffer = Encoding.ASCII.GetBytes(sEndBuffer);
                if (Debug)
                    Console.Write(sEndBuffer);
                _socket.Send(endBuffer);
                _end = true;
                return true;
            }
            string sBuffer = _rawRequestDataTemp.Substring(0, count);
            string chunkSize = count.ToString("X");
            sBuffer = chunkSize + "\r\n" + sBuffer + "\r\n";
            _rawRequestDataTemp = _rawRequestDataTemp.Remove(0, count);
            _rawRequestDataOffset += count;

            byte[] buffer = Encoding.ASCII.GetBytes(sBuffer);
            if (Debug)
                Console.Write(sBuffer);
            _socket.Send(buffer);
            return false;
        }

        /// <summary>
        /// Передать сразу все данные на сервер
        /// </summary>
        /// <returns>true, если метод не подходит (включен chunked=true режим), или все данные переданы</returns>
        public bool Send()
        {
            if (_end || _chunked)
                return true;

            byte[] buffer = Encoding.ASCII.GetBytes(_rawRequestData);
            if (Debug)
                Console.Write(_rawRequestData);
            _socket.Send(buffer);
            _end = true;
            return true;
        }

        /// <summary>
        /// Получить ответ и распарсить его 
        /// </summary>
        /// <typeparam name="T">Тип результата парсинга</typeparam>
        /// <returns>Экземпляр данных, который получаем после того, как распарсили</returns>
        public T Response<T>() where T : IApiResponse<T>, new()
        {
            byte[] buffer = new byte[1024];
            string resultString = "";
            while (true)
            {
                int received = _socket.Receive(buffer);
                resultString += Encoding.UTF8.GetString(buffer);
                if (received < 1024)
                    break;
            }
            T result = new T();
            HttpResponse response = new HttpResponse(resultString);
            StatusCode = response.StatusCode;
            result.Parse(response);

            return result;
        }


        ~HttpRequest()
        {
            _socket.Close();
        }
    }
}