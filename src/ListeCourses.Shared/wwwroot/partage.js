// Ouvre le menu de partage du système s'il existe (téléphone, Windows),
// sinon copie le lien dans le presse-papiers. Renvoie true si copié.
export async function partager(titre, lien) {
    if (navigator.share) {
        try {
            await navigator.share({ title: titre, text: `Liste de courses « ${titre} »`, url: lien });
        } catch {
            // Partage annulé par l'utilisateur : rien à faire.
        }
        return false;
    }
    await navigator.clipboard.writeText(lien);
    return true;
}
