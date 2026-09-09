using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MonkeyBook.Shared;

// In azure the frontend is a static web app on its own origin, so the apis need cors.
// Locally the vite dev server proxies the calls and no origin is configured.
public static class CorsExtensions
{
	public const string PolicyName = "frontend";

	public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
	{
		var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

		return services.AddCors(options => options.AddPolicy(PolicyName, policy =>
		{
			if (allowedOrigins.Length == 0)
			{
				return;
			}

			policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
		}));
	}

	public static WebApplication UseFrontendCors(this WebApplication app)
	{
		app.UseCors(PolicyName);

		return app;
	}
}
