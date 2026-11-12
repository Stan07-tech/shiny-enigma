namespace ListeCourses.Models;

// Outils autour des codes de partage (listes et recettes), partagés par le serveur et les clients.
public static class CodeListe
{
    // Sans 0/O ni 1/I/L pour éviter les confusions quand on recopie un code.
    public const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    public const int Longueur = 10;

    // Accepte un code seul ou un lien complet (https://.../l/CODE/rayon/...)
    // et renvoie le code en majuscules, ou null s'il n'est pas valide.
    // prefixe : "/l/" pour une liste, "/r/" pour une recette.
    public static string? Extraire(string? saisie, string prefixe = "/l/")
    {
        if (string.IsNullOrWhiteSpace(saisie)) return null;

        var texte = saisie.Trim();
        var position = texte.IndexOf(prefixe, StringComparison.OrdinalIgnoreCase);
        if (position >= 0)
        {
            texte = texte[(position + prefixe.Length)..];
            var fin = texte.IndexOfAny(['/', '?', '#']);
            if (fin >= 0) texte = texte[..fin];
        }

        var code = texte.ToUpperInvariant();
        return EstValide(code) ? code : null;
    }

    public static bool EstValide(string? code) =>
        code is { Length: Longueur } && code.All(Alphabet.Contains);
}
