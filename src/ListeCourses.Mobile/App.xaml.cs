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
		return new Window(new MainPage()) { Title = "Liste de courses" };
	}
}
