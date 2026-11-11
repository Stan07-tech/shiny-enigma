namespace ListeCourses.Mobile;

public static class Configuration
{
    // Adresse du site web déployé sur Azure (avec le "/" final).
    // À remplacer par l'adresse de votre App Service, puis régénérer l'APK.
    public static readonly Uri AdresseServeur = new("https://listecourses.azurewebsites.net/");
}
