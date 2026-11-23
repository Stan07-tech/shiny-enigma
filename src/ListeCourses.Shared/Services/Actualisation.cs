namespace ListeCourses.Services;

// État "au premier plan / en arrière-plan" de l'appli mobile, renseigné par
// MAUI (App.xaml.cs). Les pages s'en servent pour ne pas interroger le serveur
// quand le téléphone est en veille, et pour se recharger dès qu'on revient.
// Sur le site web, l'appli reste "au premier plan" : c'est la visibilité de
// l'onglet du navigateur qui compte (document.visibilityState).
public static class Actualisation
{
    // Intervalle du rechargement automatique d'une liste ouverte.
    public static readonly TimeSpan Intervalle = TimeSpan.FromSeconds(10);

    public static bool EnArrierePlan { get; private set; }

    // Déclenché quand l'appli revient au premier plan.
    public static event Action? Reprise;

    public static void PasserEnArrierePlan() => EnArrierePlan = true;

    public static void RevenirAuPremierPlan()
    {
        EnArrierePlan = false;
        Reprise?.Invoke();
    }
}
