using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dengue.watch.api.Migrations
{
    /// <inheritdoc />
    public partial class ModifyPredictedWeeklyDengueCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "predicted_date",
                table: "predicted_weekly_dengue_cases");

            migrationBuilder.AddColumn<int>(
                name: "predicted_iso_week",
                table: "predicted_weekly_dengue_cases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "predicted_iso_year",
                table: "predicted_weekly_dengue_cases",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "predicted_iso_week",
                table: "predicted_weekly_dengue_cases");

            migrationBuilder.DropColumn(
                name: "predicted_iso_year",
                table: "predicted_weekly_dengue_cases");

            migrationBuilder.AddColumn<DateOnly>(
                name: "predicted_date",
                table: "predicted_weekly_dengue_cases",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }
    }
}
