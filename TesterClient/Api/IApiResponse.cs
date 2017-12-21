namespace TesterClient.Api
{
    /// <summary>
    /// Интерфейс API ответа
    /// </summary>
    /// <typeparam name="T">Тип API ответа</typeparam>
    public interface IApiResponse<T> where T: new()
    {
        /// <summary>
        /// Распарсить ответ
        /// </summary>
        /// <param name="response">HTTP/1.1 ответ, который предварительно отпарсили</param>
        void Parse(HttpResponse response);
    }
}