# Vérification manuelle du front

Le front n'est couvert par aucun test automatisé (choix assumé, voir les limites du README).
Cette procédure le remplace : elle se déroule à la main, dans le navigateur, et prend quelques
minutes.

Rien de ce qui suit n'a été exécuté dans un navigateur par l'IA : elle n'en a pas. Les attendus
décrits ici viennent du code et des réponses du serveur, vérifiées séparément avec curl et par les
tests. Seul le script de la section 7 a réellement été lancé, contre l'API.

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

## 1. Créer une partie et poser sa flotte

1. Cliquer sur **Nouvelle partie**.
2. L'URL devient `/partie/{identifiant}`. Le **port** et les deux grilles apparaissent.

À vérifier :

- **ma grille est vide** : c'est au joueur de poser sa flotte ;
- le **port** dessine les navires restants à leur vraie longueur : une forme de 5 cases, une de 4,
  une de 3 suivie de `× 2`, une de 2. Le navire de **2 cases est sélectionné par défaut**, encadré
  de bleu ;
- la grille adverse est entièrement vide ;
- la phase affichée est `Setup`, sans tour ;
- le bouton **Commencer** est désactivé ;
- onglet Réseau : `POST /games` en `201`, `POST /battleship.GameService/GetGame` en `200` précédé
  de sa pré-vérification `OPTIONS` en `204`, puis
  `GET /games/{id}/placements?length=2&orientation=Horizontal` en `200`.

3. Cliquer sur la forme de **5 cases** dans le port, puis survoler ma grille.

À vérifier :

- l'aperçu surligne 5 cases à partir de la case survolée, **contour vert** là où le navire tient,
  **contour rouge** là où il ne tient pas (par exemple à partir de la colonne G à l'horizontale,
  où il déborderait) ;
- en sortant de la grille, l'aperçu disparaît ;
- onglet Réseau : un seul `GET …/placements?length=5&…` au moment du clic sur le port, **et aucun
  appel pendant le survol**. La couleur vient de cette liste, pas d'une règle rejouée par le
  client.

4. Appuyer sur la touche **R**, ou tourner la **molette** au-dessus des grilles.

À vérifier : l'aperçu et la forme dessinée dans le port basculent entre horizontal et vertical, et
le bouton **Orientation** suit. Le bouton fait la même chose au clic ; il reste utilisable si le
focus clavier est perdu. La molette ne fait pas défiler la page.

5. Cliquer sur une case pour poser le navire, par exemple A1 en horizontal.

À vérifier : les 5 cases deviennent une coque grise aux extrémités arrondies, la forme de 5 quitte
le port, et la version augmente d'une unité dans l'appel suivant. Onglet Réseau :
`POST /games/{id}/ships` en `200`, suivi d'un nouveau `GET …/placements`.

6. Sélectionner **4 cases** et survoler A2, juste sous le premier navire.

À vérifier : l'aperçu est **rouge**. Cliquer quand même : l'écran envoie la demande, et c'est le
serveur qui refuse — message `Refusé : Ce navire en touche un autre par un côté.`, **et la grille
ne change pas**. Onglet Réseau : `409` avec `"rejection": "AdjacentShip"`.

C'est le point important de la préparation : le rouge est une couleur, pas un verrou. Le client
n'empêche rien, il affiche ce que le serveur lui a dit.

7. Cliquer sur **n'importe quelle case** du navire de 5 déjà posé.

À vérifier : le navire entier disparaît et sa forme revient dans le port, à sa place dans l'ordre
des longueurs. Onglet Réseau : `POST /games/{id}/ships/remove` en `200`.

8. Cliquer sur **Placement aléatoire**.

À vérifier : les 5 navires apparaissent d'un coup, le port affiche `Flotte complète.`, et le
bouton **Commencer** devient actif.

## 2. Démarrer la partie

Cliquer sur **Commencer**.

À vérifier : la phase passe à `InProgress`, le tour est `Player`, et si l'ordinateur a commencé,
la ligne de message liste ses tirs d'ouverture, visibles sur ma grille. Le port et la barre de
préparation disparaissent.

Puis cliquer sur une case de **ma** grille : rien ne se passe. Un placement après le démarrage
serait refusé avec `NotInSetup`, mais l'écran n'envoie même plus la demande.

## 3. Tirer

Cliquer sur une case de la **grille adverse**.

À vérifier :

- le message annonce `À l'eau.`, `Touché.` ou `Coulé : …` ;
- les trois résultats se distinguent à l'œil : un raté est une **pastille grise** sur l'eau, une
  case touchée est **orange avec une croix**, un navire coulé passe entièrement en **rouge sombre**,
  croix comprise, y compris sur ma grille ;
- après un tir à l'eau, le message liste les tirs de l'ordinateur, et les cases correspondantes
  changent sur **ma** grille ;
- après un tir touché, c'est encore mon tour et l'ordinateur n'a pas joué ;
- onglet Réseau : `POST /games/{id}/shots` en `200`.

## 4. Un tir refusé ne change rien

Cliquer **deux fois** sur la même case de la grille adverse.

À vérifier :

- le second clic affiche `Refusé : Cette case a déjà été tirée.` ;
- la grille n'a pas changé : aucune case nouvelle, aucun tir de l'ordinateur, la phase et le tour
  sont identiques ;
- onglet Réseau : le second `POST … /shots` répond `409`, et le corps contient
  `"rejection": "AlreadyTargeted"`.

C'est le point important : le front n'anticipe rien. Tant que le serveur n'a pas répondu, aucune
case ne bouge.

## 5. Reprise après rechargement

Appuyer sur `F5` sur la page `/partie/{identifiant}`.

À vérifier : la partie revient dans le même état, tirs compris. L'identifiant vient de l'URL, et
l'état est relu par gRPC-Web.

## 6. Les deux erreurs gRPC

**Partie introuvable.** Arrêter l'API avec `Ctrl+C`, vérifier que le port est libre, la relancer,
puis recharger la page.

À vérifier : le message `Cette partie n'existe plus sur le serveur. Crée une nouvelle partie.`,
avec le bouton **Nouvelle partie**. Onglet Réseau : `grpc-status: 5` dans les en-têtes de réponse,
pour un `POST` en `200`.

**Identifiant invalide.** Ouvrir `http://localhost:5274/partie/pas-un-guid`.

À vérifier : le message `Identifiant refusé : L'identifiant de la partie n'est pas valide.`
Onglet Réseau : `grpc-status: 3`.

## 7. Fin de partie

Jouer jusqu'au bout demande une soixantaine de clics. Pour aller droit à l'écran de fin, jouer la
partie par l'API depuis un troisième terminal, puis recharger la page.

```powershell
$api = "https://localhost:7260"
$id = (Invoke-RestMethod -Method Post "$api/games").id
$body = @{ expectedVersion = 0 } | ConvertTo-Json
Invoke-RestMethod -Method Post "$api/games/$id/fleet/random" -ContentType 'application/json' -Body $body | Out-Null
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

## 8. Nouvelle partie

Cliquer sur **Nouvelle partie**.

À vérifier : l'URL change d'identifiant, la phase revient à `Setup`, ma grille est vide et le port
contient de nouveau les cinq navires, et l'ancienne partie reste accessible par son ancienne URL
tant que l'API tourne.
