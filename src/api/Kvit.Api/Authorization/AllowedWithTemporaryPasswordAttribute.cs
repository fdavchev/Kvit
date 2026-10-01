namespace Kvit.Api.Authorization
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AllowedWithTemporaryPasswordAttribute : Attribute
    {
    }
}
