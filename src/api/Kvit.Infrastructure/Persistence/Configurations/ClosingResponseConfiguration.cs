using Kvit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class ClosingResponseConfiguration : IEntityTypeConfiguration<ClosingResponse>
    {
        public void Configure(EntityTypeBuilder<ClosingResponse> builder)
        {
            builder.ToTable("closing_responses", table =>
            {
                table.HasCheckConstraint("ck_closing_responses_response", EnumCheck.OneOf<ClosingAnswer>("response"));
                table.HasCheckConstraint("ck_closing_responses_reason_length", $"char_length(reason) BETWEEN 1 AND {ClosingResponse.ReasonMaxLength}");
            });

            builder.HasKey(response => new { response.GroupId, response.MemberId });

            builder.Property(response => response.Response).HasConversion<string>();

            builder.HasOne<Group>()
                .WithMany()
                .HasForeignKey(response => response.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<GroupMember>()
                .WithMany()
                .HasForeignKey(response => response.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
