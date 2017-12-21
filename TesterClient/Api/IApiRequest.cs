namespace TesterClient.Api
{
    public interface IApiRequest
    {
        string ContentType();
        string Raw();
    }
}