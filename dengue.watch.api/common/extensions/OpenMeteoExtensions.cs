namespace dengue.watch.api.common.extensions;

public static class OpenMeteoExtensions
{
    public static IServiceCollection AddOpenMeteo(this IServiceCollection services)
    {
        services.AddHttpClient("OpenMeteo", client =>
        {
            client.BaseAddress = new Uri("https://api.open-meteo.com/");
            client.DefaultRequestHeaders.Add("User-Agent", "DengueWatchAPI/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        return services;
    }
}



