namespace dengue.watch.api.infrastructure.database;

/// <summary>
/// Options for connecting to a PostgreSQL database
/// </summary>
public class PostgresOptions
{
	public const string SectionName = "Postgres";

	public string Host { get; set; } = string.Empty;
	public int Port { get; set; } = 5432;
	public string Database { get; set; } = string.Empty;
	public string Username { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
	public bool SslModeRequire { get; set; } = true;
	public bool TrustServerCertificate { get; set; } = true;

	public string UserId {get; set;} = string.Empty;
	public string Server {get; set;} = string.Empty;
	public string ServerPort {get; set;} = string.Empty;



	/// <summary>
	/// Build an Npgsql connection string from options.
	/// </summary>
	public string ToConnectionString()
	{
		var sslMode = SslModeRequire ? "Require" : "Disable";
		var trust = TrustServerCertificate ? "true" : "false";
		return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};Ssl Mode={sslMode};Trust Server Certificate={trust}";
	}

	/// <summary>
	/// Build an Npgsql connection string from options for using Session Pooling.
	/// </summary>
	public string ToSessionPoolingConnectionString()
	{
		if (!string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(Database))
		{
			// Session pooling connection string
			return $"User Id={UserId};Password={Password};Server={Server};Port={Port};Database={Database};";
		}
		else
		{
			// Standard connection string
			return ToConnectionString();
		}
	}
}


