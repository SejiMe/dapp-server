using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dengue.watch.api.Migrations
{
    /// <inheritdoc />
    public partial class RenameToLagged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "iso_year",
                table: "predicted_weekly_dengue_cases",
                newName: "lagged_iso_year");

            migrationBuilder.RenameColumn(
                name: "iso_week",
                table: "predicted_weekly_dengue_cases",
                newName: "lagged_iso_week");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "lagged_iso_year",
                table: "predicted_weekly_dengue_cases",
                newName: "iso_year");

            migrationBuilder.RenameColumn(
                name: "lagged_iso_week",
                table: "predicted_weekly_dengue_cases",
                newName: "iso_week");
        }
    }
}
