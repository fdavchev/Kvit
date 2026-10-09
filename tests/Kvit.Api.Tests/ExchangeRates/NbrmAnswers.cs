namespace Kvit.Api.Tests.ExchangeRates
{
    public static class NbrmAnswers
    {
        public const string Date = "2026-09-24T00:00:00";

        public const string RawEurExample = """{"rBr":178,"datum":"2026-09-24T00:00:00","valuta":"978","oznaka":"EUR","nomin":1,"sreden":61.561}""";

        public static string Entry(string code, string nomin, string sreden, string datum = Date)
        {
            return $$"""{"rBr":1,"datum":"{{datum}}","valuta":"000","oznaka":"{{code}}","nomin":{{nomin}},"sreden":{{sreden}}}""";
        }

        public static string Eur(string nomin, string sreden, string datum = Date)
        {
            return Entry("EUR", nomin, sreden, datum);
        }

        public static string List(params string[] entries)
        {
            return $"[{string.Join(",", entries)}]";
        }

        public static string ListWithOtherCurrencies(string eurEntry)
        {
            return List(Entry("USD", "1", "52.8000"), eurEntry, Entry("GBP", "1", "70.2500"));
        }
    }
}
