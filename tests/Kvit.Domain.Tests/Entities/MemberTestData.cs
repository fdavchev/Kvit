using Kvit.Domain.Entities;

namespace Kvit.Domain.Tests.Entities
{
    public static class MemberTestData
    {
        public static readonly Guid GroupId = Guid.Parse("0199a1b2-c3d4-7e5f-8a6b-7c8d9e0f1a30");
        public static readonly Guid OtherUserId = Guid.Parse("0199a1b2-c3d4-7e5f-8a6b-7c8d9e0f1a2e");

        public static GroupMember Account(Guid groupId, Guid userId, string name)
        {
            return GroupMember.ForAccount(groupId, userId, name, GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-10));
        }

        public static GroupMember PlainName(Guid groupId, string name)
        {
            return GroupMember.ForName(groupId, name, GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-10)).Value;
        }

        public static GroupMember Removed(Guid groupId, Guid userId, string name)
        {
            GroupMember member = Account(groupId, userId, name);
            member.Remove(GroupTestData.OwnerId, GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-3));

            return member;
        }

        public static GroupMember RemovedPlainName(Guid groupId, string name)
        {
            GroupMember member = PlainName(groupId, name);
            member.Remove(GroupTestData.OwnerId, GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-3));

            return member;
        }

        public static GroupMember Left(Guid groupId, Guid userId, string name)
        {
            GroupMember member = Account(groupId, userId, name);
            member.Leave(GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-3));

            return member;
        }

        public static GroupMember SetAside(Guid groupId, Guid userId, string name, DateTimeOffset setAsideAt)
        {
            GroupMember member = Account(groupId, userId, name);
            member.SetAside(setAsideAt);

            return member;
        }

        public static GroupMember Claimed(Guid groupId, Guid userId, string plainName)
        {
            GroupMember member = PlainName(groupId, plainName);
            member.Claim(userId, GroupTestData.Moment.AddDays(-2));

            return member;
        }

        public static GroupRoster RosterOf(params GroupMember[] members)
        {
            return new GroupRoster([.. members.Select(Named)]);
        }

        public static NamedMember Named(GroupMember member)
        {
            return new NamedMember { Member = member, DisplayName = member.Name };
        }

        public static NamedMember Named(GroupMember member, string displayName)
        {
            return new NamedMember { Member = member, DisplayName = displayName };
        }
    }
}
