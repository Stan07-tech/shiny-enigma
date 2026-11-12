using ListeCourses.Services;

namespace ListeCourses.Mobile;

// Partage sur téléphone : ouvre le menu natif d'Android (SMS, WhatsApp, e-mail...).
public sealed class PartageMobile : IPartage
{
    public string Lien(string chemin) => new Uri(Configuration.AdresseServeur, chemin).ToString();

    public async Task<bool> PartagerAsync(string titre, string lien)
    {
        await MainThread.InvokeOnMainThreadAsync(() => Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = $"Partager « {titre} »",
            Text = $"Ma liste de courses « {titre} » :",
            Uri = lien,
        }));
        return false;
    }
}
