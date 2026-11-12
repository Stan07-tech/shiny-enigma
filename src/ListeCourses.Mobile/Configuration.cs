namespace ListeCourses.Mobile;

public static class Configuration
{
    // Adresse du site web déployé sur Azure (avec le "/" final).
    // Pour un autre serveur : changer cette adresse, puis régénérer l'APK.
    public static readonly Uri AdresseServeur = new("https://listecourses-lb200781.azurewebsites.net/");
}
