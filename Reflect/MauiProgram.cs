using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Reflect.Data;
using Reflect.Services;
using Reflect.Services.Interfaces;

namespace Reflect;

/// <summary>
/// Composition root. Everything the app resolves at runtime is registered here.
/// </summary>
public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddMudServices();

		RegisterApplicationServices(builder.Services);

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	/// <summary>
	/// Registers the persistence and business-logic layers.
	/// </summary>
	/// <remarks>
	/// The database is a singleton because it owns a single pooled SQLite
	/// connection and performs schema creation and seeding exactly once. Services
	/// are scoped, matching the scope the BlazorWebView creates for components, so
	/// they stay cheap to construct and hold no cross-session state.
	/// Every registration is against an interface, so implementations can be
	/// substituted without touching the components that consume them.
	/// </remarks>
	private static void RegisterApplicationServices(IServiceCollection services)
	{
		services.AddSingleton<IJournalDatabase, JournalDatabase>();
		services.AddScoped<IEntryService, EntryService>();
	}
}
