namespace Kvit.Infrastructure.Persistence.Configurations
{
    public static class EnumCheck
    {
        public static string OneOf<TEnum>(string column)
            where TEnum : struct, Enum
        {
            string knownNames = string.Join(", ", Enum.GetNames<TEnum>().Select(name => $"'{name}'"));

            return $"{column} IN ({knownNames})";
        }
    }
}
