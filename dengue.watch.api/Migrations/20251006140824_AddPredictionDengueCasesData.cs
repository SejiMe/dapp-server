using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dengue.watch.api.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionDengueCasesData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "predicted_weekly_dengue_cases",
                columns: table => new
                {
                    prediction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    iso_week = table.Column<int>(type: "integer", nullable: false),
                    iso_year = table.Column<int>(type: "integer", nullable: false),
                    predicted_value = table.Column<int>(type: "integer", nullable: false),
                    predicted_date = table.Column<DateOnly>(type: "date", nullable: false),
                    PsgcCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_predicted_weekly_dengue_cases", x => x.prediction_id);
                    table.ForeignKey(
                        name: "FK_predicted_weekly_dengue_cases_administrative_areas_PsgcCode",
                        column: x => x.PsgcCode,
                        principalTable: "administrative_areas",
                        principalColumn: "psgc_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_predicted_weekly_dengue_cases_PsgcCode",
                table: "predicted_weekly_dengue_cases",
                column: "PsgcCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "predicted_weekly_dengue_cases");
        }
    }
}
