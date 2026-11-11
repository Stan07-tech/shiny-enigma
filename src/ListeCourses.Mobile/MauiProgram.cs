using ListeCourses.Data;
using ListeCourses.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ListeCourses.Mobile;

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

		// Même configuration que le site web, mais la base SQLite est stockée
		// sur le téléphone, dans le dossier privé de l'application.
		var cheminBase = Path.Combine(FileSystem.AppDataDirectory, "courses.db");
		builder.Services.AddDbContextFactory<CoursesDbContext>(options =>
			options.UseSqlite($"Data Source={cheminBase}"));
		builder.Services.AddSingleton<ArticleService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Crée la base et la table au premier lancement.
		using (var db = app.Services.GetRequiredService<IDbContextFactory<CoursesDbContext>>().CreateDbContext())
		{
			db.Database.EnsureCreated();
		}

		return app;
	}
}
