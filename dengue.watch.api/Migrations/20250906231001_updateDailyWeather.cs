using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dengue.watch.api.Migrations
{
    /// <inheritdoc />
    public partial class updateDailyWeather : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_daily_weather_date_location",
                table: "daily_weather");

            migrationBuilder.DropColumn(
                name: "location",
                table: "daily_weather");

            migrationBuilder.AddColumn<string>(
                name: "psgc_code",
                table: "daily_weather",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_daily_weather_date_psgc_code",
                table: "daily_weather",
                columns: new[] { "date", "psgc_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_weather_psgc_code",
                table: "daily_weather",
                column: "psgc_code");

            migrationBuilder.AddForeignKey(
                name: "FK_daily_weather_administrative_areas_psgc_code",
                table: "daily_weather",
                column: "psgc_code",
                principalTable: "administrative_areas",
                principalColumn: "psgc_code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_daily_weather_administrative_areas_psgc_code",
                table: "daily_weather");

            migrationBuilder.DropIndex(
                name: "IX_daily_weather_date_psgc_code",
                table: "daily_weather");

            migrationBuilder.DropIndex(
                name: "IX_daily_weather_psgc_code",
                table: "daily_weather");

            migrationBuilder.DropColumn(
                name: "psgc_code",
                table: "daily_weather");

            migrationBuilder.AddColumn<string>(
                name: "location",
                table: "daily_weather",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_daily_weather_date_location",
                table: "daily_weather",
                columns: new[] { "date", "location" },
                unique: true);
        }
    }
}
