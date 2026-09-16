# Vérification manuelle du front

Le front n'est couvert par aucun test automatisé (choix assumé, voir les limites du README).
Cette procédure le remplace : elle se déroule à la main, dans le navigateur, et prend quelques
minutes.

Prérequis et commandes de lancement : voir le README. Vérifier que le port 7260 est libre avant de
lancer l'API, sinon une ancienne instance répond à sa place.

```powershell
# terminal 1
dotnet run --project BattleShip.API --launch-profile https
# terminal 2
dotnet run --project BattleShip.App --launch-profile http
```

Ouvrir `http://localhost:5274/`, puis les outils de développement, onglet Réseau, avec
« Conserver le journal ».

## 1. Créer et démarrer une partie

1. Cliquer sur **Nouvelle partie**.
2. L'URL devient `/partie/{identifiant}`, et les deux grilles apparaissent.

À vérifier :

- ma grille montre 5 navires (5, 4, 3, 3 et 2 cases) ;
- la grille adverse est entièrement vide ;
- la phase affichée est `Setup`, sans tour ;
- onglet Réseau : `POST /games` en `201`, puis `POST /battleship.GameService/GetGame` en `200`,
  précédé de sa pré-vérification `OPTIONS` en `204`.

3. Cliquer sur **Commencer**.

À vérifier : la phase passe à `InProgress`, le tour est `Player`, et si l'ordinateur a commencé,
la ligne de message liste ses tirs d'ouverture, visibles sur ma grille.

## 2. Tirer

Cliquer sur une case de la **grille adverse**.

À vérifier :

- le message annonce `À l'eau.`, `Touché.` ou `Coulé : …` ;
- après un tir à l'eau, le message liste les tirs de l'ordinateur, et les cases correspondantes
  changent sur **ma** grille ;
- après un tir touché, c'est encore mon tour et l'ordinateur n'a pas joué ;
- onglet Réseau : `POST /games/{id}/shots` en `200`.

## 3. Un tir refusé ne change rien

Cliquer **deux fois** sur la même case de la grille adverse.

À vérifier :

- le second clic affiche `Refusé : Cette case a déjà été tirée.` ;
- la grille n'a pas changé : aucune case nouvelle, aucun tir de l'ordinateur, la phase et le tour
  sont identiques ;
- onglet Réseau : le second `POST … /shots` répond `409`, et le corps contient
  `"rejection": "AlreadyTargeted"`.

C'est le point important : le front n'anticipe rien. Tant que le serveur n'a pas répondu, aucune
case ne bouge.

## 4. Reprise après rechargement

Appuyer sur `F5` sur la page `/partie/{identifiant}`.

À vérifier : la partie revient dans le même état, tirs compris. L'identifiant vient de l'URL, et
l'état est relu par gRPC-Web.

## 5. Les deux erreurs gRPC

**Partie introuvable.** Arrêter l'API avec `Ctrl+C`, vérifier que le port est libre, la relancer,
puis recharger la page.

À vérifier : le message `Cette partie n'existe plus sur le serveur. Crée une nouvelle partie.`,
avec le bouton **Nouvelle partie**. Onglet Réseau : `grpc-status: 5` dans les en-têtes de réponse,
pour un `POST` en `200`.

**Identifiant invalide.** Ouvrir `http://localhost:5274/partie/pas-un-guid`.

À vérifier : le message `Identifiant refusé : L'identifiant de la partie n'est pas valide.`
Onglet Réseau : `grpc-status: 3`.

## 6. Fin de partie

Jouer jusqu'au bout demande une soixantaine de clics. Pour aller droit à l'écran de fin, jouer la
partie par l'API depuis un troisième terminal, puis recharger la page.

```powershell
$api = "https://localhost:7260"
$id = (Invoke-RestMethod -Method Post "$api/games").id
$turn = Invoke-RestMethod -Method Post "$api/games/$id/start"
$version = $turn.state.version
$cells = foreach ($row in 0..9) { foreach ($col in 0..9) { [pscustomobject]@{ c = $col; r = $row } } }
foreach ($cell in $cells) {
  if ($turn.state.phase -eq 'Finished') { break }
  $body = @{ column = $cell.c; row = $cell.r; expectedVersion = $version } | ConvertTo-Json
  $turn = Invoke-RestMethod -Method Post "$api/games/$id/shots" -ContentType 'application/json' -Body $body
  $version = $turn.state.version
}
"http://localhost:5274/partie/$id -> $($turn.state.phase), gagnant $($turn.state.winner)"
```

Ouvrir l'URL affichée.

À vérifier : le titre `Tu as gagné.` ou `L'ordinateur a gagné.`, le bouton **Nouvelle partie**, les
deux grilles toujours visibles. Cliquer sur une case de la grille adverse affiche alors
`Refusé : La partie est terminée.`

Dernier essai lancé pendant l'écriture de cette procédure : partie terminée après 51 tirs du
joueur, gagnant `Computer`.

## 7. Nouvelle partie

Cliquer sur **Nouvelle partie**.

À vérifier : l'URL change d'identifiant, la phase revient à `Setup`, les deux grilles sont
réinitialisées, et l'ancienne partie reste accessible par son ancienne URL tant que l'API tourne.
