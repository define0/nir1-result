using System;
using System.Collections.Generic;
using TesterClient.Api;

namespace TesterClient
{
    public class TestLoad
    {
        /// <summary>
        /// Идентификатор теста
        /// </summary>
        internal int TestId;
        /// <summary>
        /// Идентификатор запускаемой программы
        /// </summary>
        private int RunId;
        /// <summary>
        /// Количество генерируемых записей за тест
        /// </summary>
        private int _entries = 10;
        /// <summary>
        /// Сервер, к которому подключается программа
        /// </summary>
        private string _host = "iamnotaprogrammer.ru";
        /// <summary>
        /// Порт, к которому подключается программа
        /// </summary>
        private int _port = 8880;
        /// <summary>
        /// Передавать по частям или нет (Transfer-Encoding: chunked) 
        /// </summary>
        private bool _chunked = false;
        /// <summary>
        /// Вести подробное логирование
        /// </summary>
        private bool _verbose = false;
        /// <summary>
        /// Вести ли отладочную информацию и отображать все запросы
        /// </summary>
        private bool _debug = false;
        /// <summary>
        /// Значение Keep-Alive 
        /// </summary>
        private int _keepAlive = 60;
        /// <summary>
        /// Минимальное количество байт, передаваемой за чанк
        /// </summary>
        private int _minSendingData = 100;
        /// <summary>
        /// Максимальное количество байт, передаваемое за чанк
        /// </summary>
        private int _maxSendingData = 200;
        /// <summary>
        /// Минимальное время задержки, между передачей чанков
        /// </summary>
        private int _minSleepTime = 1;
        /// <summary>
        /// Максимальное время задержки, между передачей чанков
        /// </summary>
        private int _maxSleepTime = 2;

        /// <summary>
        /// Делегат на сообщения, передаваемое строкой
        /// </summary>
        /// <param name="message">Сообшение</param>
        public delegate void MessageDelegate(string message);
        /// <summary>
        /// Делегат на сообщения,передаваемые исключениями
        /// </summary>
        /// <param name="exception">Исключение</param>
        public delegate void ExceptionDelegate(Exception exception);
        /// <summary>
        /// Куда направлять все исключения теса
        /// </summary>
        public ExceptionDelegate ExceptionMessage = Console.WriteLine;
        /// <summary>
        /// Куда направлять сообщения о результате
        /// </summary>
        public MessageDelegate ResultMessage = Console.WriteLine;
        /// <summary>
        /// Куда направлять отладочные сообщения
        /// </summary>
        public MessageDelegate DebugMessage = Console.WriteLine;
        /// <summary>
        /// Куда направлять отчетные сообщения
        /// </summary>
        public MessageDelegate VerboseMessage = Console.WriteLine;

        /// <summary>
        /// Инициализация теста
        /// </summary>
        /// <param name="runId">Идентификатор программы</param>
        /// <param name="testId">Идентификатор теста</param>
        public TestLoad(int runId, int testId)
        {
            TestId = testId;
            RunId = runId;
        }
        /// <summary>
        /// Конфигурация теста
        /// </summary>
        /// <param name="config">Аргументы коммандной строки, переданные программе</param>
        internal void Configure(Dictionary<string, string> config)
        {
            foreach (KeyValuePair<string, string> pair in config)
            {
                try
                {
                    switch (pair.Key.ToUpper())
                    {
                        case "/ENTRIES":
                            _entries = Convert.ToInt32(pair.Value);
                            break;
                        case "/HOST":
                            _host = pair.Value;
                            break;
                        case "/PORT":
                            _port = Convert.ToInt32(pair.Value);
                            break;
                        case "/CHUNKED":
                            _chunked = true;
                            break;
                        case "/VERBOSE":
                            _verbose = true;
                            break;
                        case "/DEBUG":
                            _debug = true;
                            break;
                        case "/KEEPALIVE":
                            _keepAlive = Convert.ToInt32(pair.Value);
                            break;
                        case "/MINSENDINGDATA":
                            _minSendingData = Convert.ToInt32(pair.Value);
                            break;
                        case "/MAXSENDINGDATA":
                            _maxSendingData = Convert.ToInt32(pair.Value);
                            break;
                        case "/MINSLEEPTIME":
                            _minSleepTime = Convert.ToInt32(pair.Value);
                            break;
                        case "/MAXSLEEPTIME":
                            _maxSleepTime = Convert.ToInt32(pair.Value);
                            break;
                    }
                }
                catch (Exception e)
                {
                    ExceptionMessage(e);
                }
            }
        }
        /// <summary>
        /// Функция основной работы теста, передаваемая на поток
        /// </summary>
        public void Callback()
        {
            try
            {
                LoadRequest loader = new LoadRequest(1, LoadRequest.Controllers.acc);
                Random rand = new Random(DateTime.Now.Millisecond);
                double number = rand.NextDouble();
                for (int i = 0; i < _entries; i++)
                {
                    if (number > 1)
                    {
                        number -= 1.0f;
                    }
                    else number *= 1.073;
                    loader.AddData(DateTime.Now.AddSeconds(-i), number);
                    if (i % 100 == 0 && _verbose)
                        VerboseMessage(string.Format("datagen: RunId={2} TestId={1} Generated={0} Total={3}", i, TestId, RunId, _entries));
                }
                DateTime startTime = DateTime.Now;
                HttpRequest http = new HttpRequest(_host, _port, _chunked)
                {
                    Debug = _debug,
                    KeepAlive = _keepAlive
                };

                http.Post("/api/v1/load", loader);
                http.SendHeader();
                while (true)
                {
                    bool flag = false;
                    if (_chunked)
                    {
                        int count = rand.Next(_minSendingData, _maxSendingData);
                        if (_verbose)
                            DebugMessage(string.Format("send: RunId={3} TestId={4} Sent={2} Current={0} Total={1}", 
                                http.ChunkedPosition,
                                http.ContentLength, 
                                count<http.ContentLength ? count : http.ContentLength,
                                RunId,
                                TestId));

                        flag = http.Send(count);
                    }
                    else
                    {
                        flag = http.Send();
                    }
                    if (flag)
                        break;
                    System.Threading.Thread.Sleep(rand.Next(_minSleepTime, _maxSleepTime));
                }

                LoadResponse loadResponse = http.Response<LoadResponse>();
                TimeSpan endTime = DateTime.Now - startTime;
                
                ResultMessage(string.Format("result: RunId={5} TestId={3} Success={0} StatusCode={1} Status={2} Time={4}ms",
                    loadResponse.Success,
                    (int) http.StatusCode,
                    http.StatusCode,
                    TestId, 
                    endTime.TotalMilliseconds,
                    RunId));
            }
            catch (Exception e)
            {
                ExceptionMessage(e);
            }
        }
    }
}