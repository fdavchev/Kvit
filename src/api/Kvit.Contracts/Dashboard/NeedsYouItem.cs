using System.Text.Json.Serialization;

namespace Kvit.Contracts.Dashboard
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(PaymentNeedsYouItem), "payment")]
    [JsonDerivedType(typeof(ClosingNeedsYouItem), "closing")]
    public abstract record NeedsYouItem(Guid GroupId, string GroupName);
}
