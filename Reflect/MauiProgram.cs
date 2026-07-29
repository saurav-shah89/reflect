using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Reflect.Data;
using Reflect.Services;
using Reflect.Services.Interfaces;

namespace Reflect;

// Sets up the app and registers all the services.
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

	// The database is a singleton because it holds one connection and only
	// creates the tables once. The services are scoped, which matches the scope
	// the BlazorWebView makes for components.
	//
	// Everything is registered against an interface so the implementations can
	// be swapped without touching the pages.
	private static void RegisterApplicationServices(IServiceCollection services)
	{
		// The library does not know where the platform keeps app data, so the app
		// supplies the path. This is the one piece of platform knowledge the
		// domain needs and the reason it can target plain net10.0.
		services.AddSingleton<IJournalDatabase>(provider => new JournalDatabase(
			Path.Combine(FileSystem.AppDataDirectory, JournalDatabase.DatabaseFileName),
			provider.GetRequiredService<ILogger<JournalDatabase>>()));

		// Reference data is cached in memory and shared across screens, so it is a
		// singleton. The Markdown pipeline is immutable and expensive to build,
		// so it is shared too.
		services.AddSingleton<IReferenceDataService, ReferenceDataService>();
		services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();

		// Lock state is per session and shared by every screen, so it is a singleton.
		services.AddSingleton<AppLockState>();

		services.AddScoped<IEntryService, EntryService>();
		services.AddScoped<IAnalyticsService, AnalyticsService>();
		services.AddScoped<ISettingsService, SettingsService>();
		services.AddScoped<IJournalExporter, PdfJournalExporter>();
	}
}
