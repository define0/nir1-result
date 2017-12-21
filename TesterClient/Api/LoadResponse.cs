namespace TesterClient.Api
{
    public class LoadResponse : IApiResponse<LoadResponse>
    {
        /// <summary>
        /// Флаг принятия данных на сервер
        /// </summary>
        public bool Success;

        /// <summary>
        /// Распарсить ответ
        /// </summary>
        /// <param name="response">HTTP/1.1 ответ, который предварительно отпарсили</param>
        public void Parse(HttpResponse response)
        {
            Success = response.RawData.Contains("\"success\":true");
        }
    }
}