using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dengue.watch.api.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "month_name",
                table: "predicted_weekly_dengue_cases",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "month_name",
                table: "predicted_weekly_dengue_cases");
        }
    }
}
