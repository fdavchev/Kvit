namespace Kvit.Application.Commands.PersonalCategories
{
    public sealed record UpdatePersonalCategoryCommand(Guid CategoryId, string Name, string Emoji, string Color);
}
