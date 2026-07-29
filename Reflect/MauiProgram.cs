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
		// Reflect.Core has no idea where the app data folder is, so the path gets
		// passed in from here. That was the only bit of MAUI it needed, which is
		// why it can target plain net10.0.
		services.AddSingleton<IJournalDatabase>(provider => new JournalDatabase(
			Path.Combine(FileSystem.AppDataDirectory, JournalDatabase.DatabaseFileName),
			provider.GetRequiredService<ILogger<JournalDatabase>>()));

		// Singletons - the reference data is cached and shared by every screen,
		// and the Markdown pipeline is slow to build so it's only made once.
		services.AddSingleton<IReferenceDataService, ReferenceDataService>();
		services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();

		// One lock state for the whole session.
		services.AddSingleton<AppLockState>();

		services.AddScoped<IEntryService, EntryService>();
		services.AddScoped<IAnalyticsService, AnalyticsService>();
		services.AddScoped<ISettingsService, SettingsService>();
		services.AddScoped<IJournalExporter, PdfJournalExporter>();
	}
}
