using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dengue.watch.api.Migrations
{
    /// <inheritdoc />
    public partial class AddedStatisticsRelated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "confidence_percentage",
                table: "predicted_weekly_dengue_cases",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<float>(
                name: "lower_bound",
                table: "predicted_weekly_dengue_cases",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<double>(
                name: "probability_of_outbreak",
                table: "predicted_weekly_dengue_cases",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "risk_level",
                table: "predicted_weekly_dengue_cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "upper_bound",
                table: "predicted_weekly_dengue_cases",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "confidence_percentage",
                table: "predicted_weekly_dengue_cases");

            migrationBuilder.DropColumn(
                name: "lower_bound",
                table: "predicted_weekly_dengue_cases");

            migrationBuilder.DropColumn(
                name: "probability_of_outbreak",
                table: "predicted_weekly_dengue_cases");

            migrationBuilder.DropColumn(
                name: "risk_level",
                table: "predicted_weekly_dengue_cases");

            migrationBuilder.DropColumn(
                name: "upper_bound",
                table: "predicted_weekly_dengue_cases");
        }
    }
}
