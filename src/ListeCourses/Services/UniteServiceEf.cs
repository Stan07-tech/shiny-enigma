using ListeCourses.Data;
using ListeCourses.Models;
using Microsoft.EntityFrameworkCore;

namespace ListeCourses.Services;

// Version serveur : SELECT * FROM Unites ORDER BY Ordre, gardé en mémoire.
// Après un ajout d'unité directement en base (SSMS), redémarrer le site pour la voir.
public class UniteServiceEf(IDbContextFactory<CoursesDbContext> dbFactory) : IUniteService
{
    private List<Unite>? cache;

    public async Task<List<Unite>> ListerAsync()
    {
        if (cache is not null) return cache;
        await using var db = await dbFactory.CreateDbContextAsync();
        return cache = await db.Unites.AsNoTracking().OrderBy(u => u.Ordre).ThenBy(u => u.Symbole).ToListAsync();
    }
}
