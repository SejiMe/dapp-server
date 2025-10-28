using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dengue.watch.api.infrastructure.database;

/// <summary>
/// Master data: administrative areas (PSGC/PSA)
/// </summary>
public class AdministrativeArea
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	[MaxLength(10)]
	public string PsgcCode { get; set; } = string.Empty;

	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	[Required]
	[MaxLength(50)]
	public string GeographicLevel { get; set; } = string.Empty; // e.g., Region/Province/City/Municipality/Barangay

	[MaxLength(500)]
	public string? OldNames { get; set; }

	public decimal? Latitude { get; set; }
	public decimal? Longitude { get; set; }

	public ICollection<WeeklyDengueCase> WeeklyDengueCases { get; set; } = new List<WeeklyDengueCase>();
	public ICollection<MonthlyDengueCase> MonthlyDengueCases { get; set; } = new List<MonthlyDengueCase>();
	public ICollection<DailyWeather> DailyWeather { get; set; } = new List<DailyWeather>();
	public ICollection<PredictedWeeklyDengueCase> PredictedWeeklyDengueCases { get; set; } = new List<PredictedWeeklyDengueCase>();

}

/// <summary>
/// Master data: WMO Weather Codes
/// </summary>
public class WeatherCode
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public int Id { get; set; }

	[Required]
	[MaxLength(200)]
	public string MainDescription { get; set; } = string.Empty;

	[MaxLength(400)]
	public string? SubDescription { get; set; }

	public ICollection<DailyWeather> DailyWeather { get; set; } = new List<DailyWeather>();
}

/// <summary>
/// Transactional: daily weather observations per location/date
/// </summary>
public class DailyWeather
{
	[Key]
	public long Id { get; set; }

	[Required]
	public DateTime Date { get; set; }

	[Required]
	[MaxLength(10)]
	public string PsgcCode { get; set; } = string.Empty;

	[Required]
	public int WeatherCodeId { get; set; }

	[ForeignKey(nameof(WeatherCodeId))]
	public WeatherCode? WeatherCode { get; set; }

	public float Temperature { get; set; }
	public float Precipitation { get; set; }
	public float Humidity { get; set; }

	[ForeignKey(nameof(PsgcCode))]
	public AdministrativeArea? AdministrativeArea { get; set; }
}

/// <summary>
/// Weekly dengue cases per location/week
/// </summary>
public class WeeklyDengueCase
{
	[Key]
	public long Id { get; set; }

	[Required]
	[MaxLength(10)]
	public string PsgcCode { get; set; } = string.Empty;

	[ForeignKey(nameof(PsgcCode))]
	public AdministrativeArea? AdministrativeArea { get; set; }

	[Required]
	public int Year { get; set; }

	[Required]
	[Range(1, 53)]
	public int WeekNumber { get; set; }

	[Required]
	public int CaseCount { get; set; }
}

/// <summary>
/// Monthly dengue cases per location/month
/// </summary>
public class MonthlyDengueCase
{
	[Key]
	public long Id { get; set; }

	[Required]
	[MaxLength(10)]
	public string PsgcCode { get; set; } = string.Empty;

	[ForeignKey(nameof(PsgcCode))]
	public AdministrativeArea? AdministrativeArea { get; set; }

	[Required]
	public int Year { get; set; }

	[Required]
	[Range(1, 12)]
	public int MonthNumber { get; set; }

	[Required]
	public int CaseCount { get; set; }
	
	
}


/// <summary>
/// weekly dengue cases per location/month
/// </summary>
public class PredictedWeeklyDengueCase
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public Guid PredictionId { get; set; }

	[Required]
	[Range(1, 53)]
	public int LaggedIsoWeek { get; set; }

	[Required]
	public int LaggedIsoYear { get; set; }

	[Required]
	public int PredictedValue { get; set; }
	
	[Required]
	[Range(1, 53)]
	public int PredictedIsoWeek { get; set; }
	
	[Required]
	public int PredictedIsoYear { get; set; }
	
	
	public float LowerBound { get; set; }
	public float UpperBound { get; set; }
	public double ConfidencePercentage { get; set; }
	public double ProbabilityOfOutbreak { get; set; }

	public string RiskLevel { get; set; }
	

	[Required]
	[MaxLength(10)]
	public string PsgcCode { get; set; } = string.Empty;

	[ForeignKey(nameof(PsgcCode))]
	public AdministrativeArea? AdministrativeArea { get; set; }

}
public static class EntityModelConfiguration
{
	public static void ConfigureEntities(this ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<AdministrativeArea>(entity =>
		{
			entity.ToTable("administrative_areas");
			entity.HasKey(p => p.PsgcCode).HasName("PK_administrative_areas");
			entity.Property(p => p.PsgcCode).HasColumnName("psgc_code");
			entity.Property(p => p.Name).HasColumnName("name");
			entity.Property(p => p.GeographicLevel).HasColumnName("geographic_level");
			entity.Property(p => p.OldNames).HasColumnName("old_names");
		});

		modelBuilder.Entity<WeatherCode>(entity =>
		{
			entity.ToTable("weather_codes");
			entity.Property(p => p.Id).HasColumnName("id");
			entity.Property(p => p.MainDescription).HasColumnName("main_description");
			entity.Property(p => p.SubDescription).HasColumnName("sub_description");
		});

		modelBuilder.Entity<DailyWeather>(entity =>
		{
			entity.ToTable("daily_weather");
			entity.HasIndex(x => new { x.Date, x.PsgcCode }).IsUnique();
			entity.Property(p => p.Date).HasColumnName("date");
			entity.Property(p => p.PsgcCode).HasColumnName("psgc_code");
			entity.Property(p => p.WeatherCodeId).HasColumnName("weather_code_id");
			entity.Property(p => p.Temperature).HasColumnName("temperature");
			entity.Property(p => p.Precipitation).HasColumnName("precipitation");
			entity.Property(p => p.Humidity).HasColumnName("humidity");
			entity.HasOne(d => d.WeatherCode)
				.WithMany(w => w.DailyWeather)
				.HasForeignKey(d => d.WeatherCodeId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(d => d.AdministrativeArea)
				.WithMany(a => a.DailyWeather)
				.HasForeignKey(d => d.PsgcCode)
				.OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<WeeklyDengueCase>(entity =>
		{
			entity.ToTable("weekly_dengue_cases");
			entity.Property(p => p.PsgcCode).HasColumnName("psgc_code");
			entity.Property(p => p.Year).HasColumnName("year");
			entity.Property(p => p.WeekNumber).HasColumnName("week_number");
			entity.Property(p => p.CaseCount).HasColumnName("case_count");
			entity.HasOne(w => w.AdministrativeArea)
				.WithMany(a => a.WeeklyDengueCases)
				.HasForeignKey(w => w.PsgcCode)
				.OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<MonthlyDengueCase>(entity =>
		{
			entity.ToTable("monthly_dengue_cases");
			entity.HasIndex(x => new { x.Year, x.MonthNumber, x.PsgcCode }).IsUnique();
			entity.Property(p => p.PsgcCode).HasColumnName("psgc_code");
			entity.Property(p => p.Year).HasColumnName("year");
			entity.Property(p => p.MonthNumber).HasColumnName("month_number");
			entity.Property(p => p.CaseCount).HasColumnName("case_count");
			entity.HasOne(m => m.AdministrativeArea)
				.WithMany(a => a.MonthlyDengueCases)
				.HasForeignKey(m => m.PsgcCode)
				.OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<PredictedWeeklyDengueCase>(entity =>
		{
			entity.ToTable("predicted_weekly_dengue_cases");
			entity.Property(p => p.PredictionId)
			.HasColumnName("prediction_id")
			.HasValueGenerator<Guid7ValueGeneratorExtension>()
			.ValueGeneratedOnAdd();
		
			entity.Property(p => p.LaggedIsoWeek).HasColumnName("lagged_iso_week");
			entity.Property(p => p.LaggedIsoYear).HasColumnName("lagged_iso_year");
			entity.Property(p => p.PredictedIsoWeek).HasColumnName("predicted_iso_week");
			entity.Property(p => p.PredictedIsoYear).HasColumnName("predicted_iso_year");
			entity.Property(p => p.PredictedValue).HasColumnName("predicted_value");
			
			entity.Property(p => p.LowerBound).HasColumnName("lower_bound");
			entity.Property(p => p.UpperBound).HasColumnName("upper_bound");
			entity.Property(p => p.ConfidencePercentage).HasColumnName("confidence_percentage");
			entity.Property(p => p.ProbabilityOfOutbreak).HasColumnName("probability_of_outbreak");
			entity.Property(p => p.RiskLevel).HasColumnName("risk_level");
			
			
			entity.HasOne(m => m.AdministrativeArea)
			.WithMany(a => a.PredictedWeeklyDengueCases)
			.HasForeignKey(m => m.PsgcCode)
			.OnDelete(DeleteBehavior.Restrict);
		});
	}
}


