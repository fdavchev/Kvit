using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public static class GuardedWrites
    {
        public const string ClosingState = "Closing";
        public const string FinishedState = "Finished";

        public static IReadOnlyList<GuardedWrite> All { get; } =
        [
            .. ExpenseWrites.All,
            .. PaymentWrites.All,
            .. GroupWrites.All,
            .. MemberWrites.All,
        ];

        public static TheoryData<string> NamesCalledByMembers => NamesOf(All.Where(write => write.Who != WhoMayCall.OwnerOnly));

        public static TheoryData<string> NamesCalledByMembersAndRefusedWhenFinished => NamesOf(All.Where(write => write.Who != WhoMayCall.OwnerOnly && !write.IsAllowedWhenFinished));

        public static TheoryData<string> NamesCalledByTheOwner => NamesOf(All.Where(write => write.Who != WhoMayCall.MemberOnly));

        public static TheoryData<string> NamesCalledByTheOwnerAndRefusedWhenFinished => NamesOf(All.Where(write => write.Who != WhoMayCall.MemberOnly && !write.IsAllowedWhenFinished));

        public static TheoryData<string> NamesThatCancelAClosing => NamesOf(All.Where(write => write.Who != WhoMayCall.MemberOnly && write.EffectOnClosing == EffectOnClosing.CancelsIt));

        public static TheoryData<string> NamesThatLeaveAClosingAlone => NamesOf(All.Where(write => write.Who != WhoMayCall.MemberOnly && write.EffectOnClosing == EffectOnClosing.LeavesItAlone));

        public static TheoryData<string> NamesAllowedWhenFinished => NamesOf(All.Where(write => write.IsAllowedWhenFinished));

        public static TheoryData<string, string> EveryNameInEveryRefusingState => CrossWithStates(All);

        public static TheoryData<string, string> NamesWithACodeForAnotherMemberInEveryRefusingState => CrossWithStates(All.Where(write => write.CodeWhenAnotherMemberTries is not null));

        public static GuardedWrite Named(string name)
        {
            return All.SingleOrDefault(write => write.Name == name)
                ?? throw new ArgumentException($"No guarded write is named '{name}'. The names are: {string.Join(", ", All.Select(write => write.Name))}.", nameof(name));
        }

        private static TheoryData<string> NamesOf(IEnumerable<GuardedWrite> writes)
        {
            TheoryData<string> data = [];
            foreach (GuardedWrite write in writes)
            {
                data.Add(write.Name);
            }

            return data;
        }

        private static TheoryData<string, string> CrossWithStates(IEnumerable<GuardedWrite> writes)
        {
            TheoryData<string, string> data = [];
            foreach (GuardedWrite write in writes)
            {
                data.Add(write.Name, ClosingState);
                data.Add(write.Name, FinishedState);
            }

            return data;
        }
    }
}
