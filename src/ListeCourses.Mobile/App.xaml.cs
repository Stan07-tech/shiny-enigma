using ListeCourses.Services;

namespace ListeCourses.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// Thème clair imposé, quel que soit le réglage du téléphone.
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var fenetre = new Window(new MainPage()) { Title = "Liste de courses" };

		// Appli en arrière-plan : les listes ouvertes cessent d'interroger le serveur.
		// Retour au premier plan : elles se rechargent aussitôt.
		fenetre.Stopped += (_, _) => Actualisation.PasserEnArrierePlan();
		fenetre.Resumed += (_, _) => Actualisation.RevenirAuPremierPlan();

		return fenetre;
	}
}
