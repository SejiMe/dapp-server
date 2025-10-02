using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dengue.watch.api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTheIndexForWeeklyDengueCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_weekly_dengue_cases_year_week_number_psgc_code",
                table: "weekly_dengue_cases");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_weekly_dengue_cases_year_week_number_psgc_code",
                table: "weekly_dengue_cases",
                columns: new[] { "year", "week_number", "psgc_code" },
                unique: true);
        }
    }
}
