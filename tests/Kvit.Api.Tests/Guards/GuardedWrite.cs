namespace Kvit.Api.Tests.Guards
{
    public sealed class GuardedWrite
    {
        public required string Name { get; init; }

        public required WhoMayCall Who { get; init; }

        public required PrepareWrite PrepareAsync { get; init; }

        public EffectOnClosing EffectOnClosing { get; init; } = EffectOnClosing.LeavesItAlone;

        public bool NeedsPendingPayment { get; init; }

        public bool IsAllowedWhenFinished { get; init; }

        public bool HasNoMemberToConfirmBeforeTheWrite { get; init; }

        public string? CodeWhenAnotherMemberTries { get; init; }
    }
}
