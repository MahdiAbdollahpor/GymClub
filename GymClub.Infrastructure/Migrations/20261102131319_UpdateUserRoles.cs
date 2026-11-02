using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Users SET Role = 5 WHERE Role = 2;");
            migrationBuilder.Sql("UPDATE Users SET Role = 4 WHERE Role = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
