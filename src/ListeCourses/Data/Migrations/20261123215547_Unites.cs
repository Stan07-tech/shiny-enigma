using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ListeCourses.Data.Migrations
{
    /// <inheritdoc />
    public partial class Unites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Table de référence des unités, avec ses valeurs.
            migrationBuilder.CreateTable(
                name: "Unites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbole = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unites", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Unites",
                columns: new[] { "Id", "Libelle", "Ordre", "Symbole" },
                values: new object[,]
                {
                    { 1, "pièce(s)", 1, "Pce" },
                    { 2, "Gramme(s)", 2, "g" },
                    { 3, "Kilogramme(s)", 3, "Kg" },
                    { 4, "Litres", 4, "L" },
                    { 5, "Centilitres", 5, "cL" },
                    { 6, "Cuillère à soupe", 6, "CaS" },
                    { 7, "Cuillère à café", 7, "CaC" },
                    { 8, "Pincée", 8, "pn" }
                });

            // 2. Articles : quantité décimale (0,5 Kg) et unité.
            migrationBuilder.AlterColumn<decimal>(
                name: "Quantite",
                table: "Articles",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "UniteId",
                table: "Articles",
                type: "int",
                nullable: true);

            // 3. Ingrédients : la quantité était un texte libre ("250 g"). On ne peut
            //    pas le convertir directement en nombre : on passe par une colonne
            //    temporaire, remplie par la requête T-SQL ci-dessous.
            migrationBuilder.AddColumn<decimal>(
                name: "QuantiteValeur",
                table: "Ingredients",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UniteId",
                table: "Ingredients",
                type: "int",
                nullable: true);

            // Découpe "250 g", "250g", "1,5 l", "1 pincée" en nombre + unité.
            // L'unité est reconnue par son symbole ou son libellé (singulier ou pluriel),
            // sans tenir compte des majuscules. Si le texte n'est pas reconnu
            // ("une poignée", "2 sachets"), il est ajouté au nom pour ne rien perdre :
            // "Amandes (une poignée)".
            migrationBuilder.Sql("""
                UPDATE i SET
                    QuantiteValeur = CASE WHEN r.Reconnu = 1 THEN n.Valeur END,
                    UniteId        = CASE WHEN r.Reconnu = 1 THEN n.UniteId END,
                    Nom            = CASE WHEN r.Reconnu = 1 THEN i.Nom
                                          ELSE LEFT(i.Nom + N' (' + t.Texte + N')', 100) END
                FROM Ingredients i
                CROSS APPLY (SELECT LTRIM(RTRIM(i.Quantite)) AS Texte,
                                    PATINDEX(N'%[^0-9.,]%', LTRIM(RTRIM(i.Quantite))) AS Pos) t
                CROSS APPLY (SELECT CASE WHEN t.Pos = 0 THEN t.Texte ELSE LEFT(t.Texte, t.Pos - 1) END AS Nombre,
                                    CASE WHEN t.Pos = 0 THEN N'' ELSE LTRIM(SUBSTRING(t.Texte, t.Pos, 30)) END AS Reste) d
                CROSS APPLY (SELECT TRY_CONVERT(decimal(9,2), REPLACE(d.Nombre, N',', N'.')) AS Valeur,
                                    (SELECT TOP 1 u.Id FROM Unites u
                                     WHERE d.Reste <> N''
                                       AND (u.Symbole = d.Reste
                                            OR REPLACE(u.Libelle, N'(s)', N'') = d.Reste
                                            OR REPLACE(u.Libelle, N'(s)', N's') = d.Reste
                                            OR u.Libelle = d.Reste + N's')
                                     ORDER BY u.Id) AS UniteId) n
                CROSS APPLY (SELECT CASE WHEN n.Valeur > 0 AND (d.Reste = N'' OR n.UniteId IS NOT NULL)
                                         THEN 1 ELSE 0 END AS Reconnu) r
                WHERE i.Quantite IS NOT NULL AND LTRIM(RTRIM(i.Quantite)) <> N'';
                """);

            migrationBuilder.DropColumn(
                name: "Quantite",
                table: "Ingredients");

            migrationBuilder.RenameColumn(
                name: "QuantiteValeur",
                table: "Ingredients",
                newName: "Quantite");

            // 4. Clés étrangères vers Unites (ON DELETE NO ACTION).
            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_UniteId",
                table: "Ingredients",
                column: "UniteId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_UniteId",
                table: "Articles",
                column: "UniteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Unites_UniteId",
                table: "Articles",
                column: "UniteId",
                principalTable: "Unites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ingredients_Unites_UniteId",
                table: "Ingredients",
                column: "UniteId",
                principalTable: "Unites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Retour arrière : la quantité redevient un texte ("250 g").
            migrationBuilder.DropForeignKey(name: "FK_Articles_Unites_UniteId", table: "Articles");
            migrationBuilder.DropForeignKey(name: "FK_Ingredients_Unites_UniteId", table: "Ingredients");
            migrationBuilder.DropIndex(name: "IX_Articles_UniteId", table: "Articles");
            migrationBuilder.DropIndex(name: "IX_Ingredients_UniteId", table: "Ingredients");

            migrationBuilder.AddColumn<string>(
                name: "QuantiteTexte",
                table: "Ingredients",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE i SET QuantiteTexte = LEFT(
                    LTRIM(CONCAT(REPLACE(FORMAT(i.Quantite, N'0.##'), N'.', N','), N' ', u.Symbole)), 30)
                FROM Ingredients i LEFT JOIN Unites u ON u.Id = i.UniteId
                WHERE i.Quantite IS NOT NULL OR i.UniteId IS NOT NULL;
                """);

            migrationBuilder.DropColumn(name: "Quantite", table: "Ingredients");
            migrationBuilder.RenameColumn(name: "QuantiteTexte", table: "Ingredients", newName: "Quantite");
            migrationBuilder.DropColumn(name: "UniteId", table: "Ingredients");
            migrationBuilder.DropColumn(name: "UniteId", table: "Articles");

            migrationBuilder.AlterColumn<int>(
                name: "Quantite",
                table: "Articles",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,2)",
                oldPrecision: 9,
                oldScale: 2);

            migrationBuilder.DropTable(name: "Unites");
        }
    }
}
