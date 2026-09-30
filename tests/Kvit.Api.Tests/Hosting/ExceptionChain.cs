namespace Kvit.Api.Tests.Hosting
{
    public static class ExceptionChain
    {
        public static List<Exception> Of(Exception exception)
        {
            List<Exception> chain = [];
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                chain.Add(current);
            }

            return chain;
        }
    }
}
