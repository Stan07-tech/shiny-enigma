using ListeCourses.Services;
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

		// L'appli n'a plus de base locale : elle utilise l'API du serveur
		// (HttpClient) et reçoit les modifications en temps réel (SignalR).
		// 30 s par requête (les lectures sont réessayées 3 fois) : le temps qu'un
		// serveur Azure en veille se réveille.
		builder.Services.AddSingleton<IListeService>(_ => new ListeServiceApi(
			new HttpClient { BaseAddress = Configuration.AdresseServeur, Timeout = TimeSpan.FromSeconds(30) },
			ListeServiceApi.CreerConnexionHub(Configuration.AdresseServeur)));
		builder.Services.AddSingleton<IRecetteService>(_ => new RecetteServiceApi(
			new HttpClient { BaseAddress = Configuration.AdresseServeur, Timeout = TimeSpan.FromSeconds(30) }));
		builder.Services.AddScoped<IListesConnues, ListesConnuesNavigateur>();
		builder.Services.AddScoped<IRecettesConnues, RecettesConnuesNavigateur>();
		builder.Services.AddSingleton<IPartage, PartageMobile>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
