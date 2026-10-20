using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersonalCategoryNameUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ix_categories_owner_user_id_active_lower_name ON categories (owner_user_id, lower(name)) "
                + "WHERE owner_user_id IS NOT NULL AND archived_at IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX ix_categories_owner_user_id_active_lower_name;");
        }
    }
}
