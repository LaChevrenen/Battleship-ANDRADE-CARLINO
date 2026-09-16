namespace BattleShip.App.Components;

// Partagé par la grille et le port, qui doivent dessiner la même coque : si les deux
// calculaient leurs coins séparément, un navire changerait de forme en passant de l'un à l'autre.
internal static class HullShape
{
    // Un coin n'est arrondi que si ses DEUX côtés sont des bords du navire. Raisonner côté par
    // côté arrondirait les quatre coins de chaque case d'un navire horizontal, qui n'a de voisine
    // ni au-dessus ni en dessous : on obtiendrait une file de ronds au lieu d'une coque.
    public static string Corners(bool gauche, bool droite, bool haut, bool bas)
    {
        var coins = new List<string>(4);

        if (gauche && haut) coins.Add("coin-hg");
        if (droite && haut) coins.Add("coin-hd");
        if (gauche && bas) coins.Add("coin-bg");
        if (droite && bas) coins.Add("coin-bd");

        return string.Join(' ', coins);
    }
}
