using System.ComponentModel.DataAnnotations;

namespace Kvit.Contracts.Validation
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class NotEmptyGuidAttribute() : ValidationAttribute("The {0} field must be a GUID other than all zeros.")
    {
        public override bool IsValid(object? value)
        {
            return value is Guid guid && guid != Guid.Empty;
        }
    }
}
