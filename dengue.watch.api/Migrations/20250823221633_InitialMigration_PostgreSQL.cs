using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace dengue.watch.api.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration_PostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "administrative_areas",
                columns: table => new
                {
                    psgc_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    geographic_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    old_names = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administrative_areas", x => x.psgc_code);
                });

            migrationBuilder.CreateTable(
                name: "DengueAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DengueAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "weather_codes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    main_description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sub_description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "monthly_dengue_cases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    psgc_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month_number = table.Column<int>(type: "integer", nullable: false),
                    case_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_dengue_cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_monthly_dengue_cases_administrative_areas_psgc_code",
                        column: x => x.psgc_code,
                        principalTable: "administrative_areas",
                        principalColumn: "psgc_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "weekly_dengue_cases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    psgc_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    week_number = table.Column<int>(type: "integer", nullable: false),
                    case_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_dengue_cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weekly_dengue_cases_administrative_areas_psgc_code",
                        column: x => x.psgc_code,
                        principalTable: "administrative_areas",
                        principalColumn: "psgc_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "daily_weather",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    weather_code_id = table.Column<int>(type: "integer", nullable: false),
                    temperature = table.Column<float>(type: "real", nullable: false),
                    precipitation = table.Column<float>(type: "real", nullable: false),
                    humidity = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_weather", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_weather_weather_codes_weather_code_id",
                        column: x => x.weather_code_id,
                        principalTable: "weather_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_weather_date_location",
                table: "daily_weather",
                columns: new[] { "date", "location" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_weather_weather_code_id",
                table: "daily_weather",
                column: "weather_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_DengueAlerts_IsActive",
                table: "DengueAlerts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DengueAlerts_Level",
                table: "DengueAlerts",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_DengueAlerts_Location",
                table: "DengueAlerts",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_monthly_dengue_cases_psgc_code",
                table: "monthly_dengue_cases",
                column: "psgc_code");

            migrationBuilder.CreateIndex(
                name: "IX_monthly_dengue_cases_year_month_number_psgc_code",
                table: "monthly_dengue_cases",
                columns: new[] { "year", "month_number", "psgc_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weekly_dengue_cases_psgc_code",
                table: "weekly_dengue_cases",
                column: "psgc_code");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_dengue_cases_year_week_number_psgc_code",
                table: "weekly_dengue_cases",
                columns: new[] { "year", "week_number", "psgc_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_weather");

            migrationBuilder.DropTable(
                name: "DengueAlerts");

            migrationBuilder.DropTable(
                name: "monthly_dengue_cases");

            migrationBuilder.DropTable(
                name: "weekly_dengue_cases");

            migrationBuilder.DropTable(
                name: "weather_codes");

            migrationBuilder.DropTable(
                name: "administrative_areas");
        }
    }
}
