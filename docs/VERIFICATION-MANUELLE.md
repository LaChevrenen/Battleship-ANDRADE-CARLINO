# Vérification manuelle du front

Le front n'est couvert par aucun test automatisé (choix assumé, voir les limites du README).
Cette procédure le remplace : elle se déroule à la main, dans le navigateur, et prend quelques
minutes.

Rien de ce qui suit n'a été exécuté dans un navigateur par l'IA : elle n'en a pas. Les attendus
décrits ici viennent du code et des réponses du serveur, vérifiées séparément avec curl et par les
tests. Seul le script de la section 10 a réellement été lancé, contre l'API.

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

## 1. L'accueil

À vérifier :

- le sélecteur **Niveau de l'ordinateur** (Facile, Normal, Difficile), réglé sur **Normal**, et le
  bouton **Nouvelle partie** ;
- la liste des parties, vide au premier lancement ;
- onglet Réseau : `GET /games` en `200`.

## 2. Créer une partie et poser sa flotte

1. Choisir un niveau, puis cliquer sur **Nouvelle partie**.
2. L'URL devient `/partie/{identifiant}`.

À vérifier :

- **ma grille est vide** : c'est au joueur de poser sa flotte ;
- le **port** dessine les navires restants à leur vraie longueur : 5, 4, 3, 3 et 2 cases ;
- le bandeau de tour et le panneau **État** affichent la phase `Setup` ;
- **Commencer** est désactivé, et **Réinitialiser** aussi tant que rien n'est posé ;
- onglet Réseau : `POST /games` en `201`, `POST /battleship.GameService/GetGame` en `200` précédé
  de sa pré-vérification `OPTIONS` en `204`.

3. Cliquer une forme dans le port, puis survoler ma grille.

À vérifier :

- l'aperçu surligne le bon nombre de cases à partir de la case survolée, **contour vert** là où le
  navire tient, **contour rouge** là où il ne tient pas ;
- en sortant de la grille, l'aperçu disparaît ;
- onglet Réseau : un `GET …/placements?length=…&orientation=…` au moment du clic sur le port, et
  **aucun appel pendant le survol**. La couleur vient de cette liste, pas d'une règle rejouée par
  le client.

4. **Laisser la souris immobile sur une case et tourner la molette.**

À vérifier : le tracé **pivote sur place, sans qu'il faille bouger la souris**, et change de
couleur si la nouvelle orientation ne tient pas. Un `…/placements` part à chaque cran de molette.

5. Cliquer pour poser le navire.

À vérifier : la coque apparaît en gris, arrondie seulement aux deux extrémités et **sans trait
entre les cases**, la forme quitte le port, et `POST /games/{id}/ships` répond `200`.

6. Survoler une case où le navire toucherait le précédent, puis cliquer quand même.

À vérifier : l'aperçu est **rouge**, et c'est le **serveur** qui refuse —
`Ce navire en touche un autre par un côté.` — **sans que la grille change**. Onglet Réseau :
`409` avec `"rejection": "AdjacentShip"`.

C'est le point important : le rouge est une couleur, pas un verrou. Le client n'empêche rien.

## 3. Reprendre un navire posé

1. Cliquer sur **n'importe quelle case** d'un navire déjà posé.

À vérifier :

- le navire **quitte la grille** et sa forme revient dans le port : il est repris en main ;
- son tracé réapparaît **immédiatement** à l'endroit d'où il vient, sans bouger la souris ;
- onglet Réseau : `POST /games/{id}/ships/remove` en `200`.

2. Tourner la molette, puis cliquer ailleurs pour le reposer.

À vérifier : il se repose dans la nouvelle orientation, et `POST /games/{id}/ships` répond `200`.

3. **Passer la molette au-dessus d'un navire resté sur la grille**, sans l'avoir repris.

À vérifier : **il ne pivote pas**. Un navire posé ne tourne jamais sur le plateau ; il faut
d'abord le reprendre en cliquant dessus.

## 4. Placement aléatoire et réinitialisation

- **Placement aléatoire** : les cinq navires apparaissent d'un coup, le port affiche
  `Flotte complète.`, et **Commencer** devient actif.
- **Réinitialiser** : la grille se vide et les cinq longueurs reviennent au port.

## 5. Démarrer la partie

Cliquer sur **Commencer**.

À vérifier : la phase passe à `InProgress`, le port et la barre de préparation disparaissent, et
si l'ordinateur a commencé, ses tirs d'ouverture sont visibles sur ma grille.

Puis cliquer sur une case de **ma** grille : rien ne se passe. Un placement après le démarrage
serait refusé avec `NotInSetup`, mais l'écran n'envoie même plus la demande.

## 6. Tirer

Cliquer sur une case de la **grille adverse**.

À vérifier :

- le message annonce `À l'eau.`, `Touché.` ou `Coulé : …` ;
- les trois résultats se distinguent à l'œil : un raté est une pastille sur l'eau, une case
  touchée est orange, un navire coulé passe entièrement en rouge sombre, y compris sur ma grille ;
- après un tir à l'eau, le message liste les tirs de l'ordinateur ;
- après un tir touché, c'est encore mon tour ;
- onglet Réseau : `POST /games/{id}/shots` en `200`.

**Audio** : décocher **Sons** puis tirer — aucun effet sonore. Recocher, le son revient. Même
chose pour **Musique**.

## 7. Un tir refusé ne change rien

Cliquer **deux fois** sur la même case de la grille adverse.

À vérifier :

- le second clic affiche `Cette case a déjà été tirée.` ;
- la grille n'a pas changé : aucune case nouvelle, aucun tir de l'ordinateur, la phase et le tour
  sont identiques ;
- onglet Réseau : le second `POST … /shots` répond `409`, corps `"rejection": "AlreadyTargeted"`.

Le front n'anticipe rien : tant que le serveur n'a pas répondu, aucune case ne bouge.

## 8. Reprise, historique et suppression

1. Appuyer sur `F5` sur `/partie/{identifiant}` : la partie revient dans le même état, tirs
   compris. L'identifiant vient de l'URL, et l'état est relu par gRPC-Web.
2. Revenir à l'accueil : la partie apparaît dans la liste, avec son état, sa date, son nombre de
   tirs et sa durée. **Reprendre** rouvre son URL.
3. Supprimer une partie depuis l'accueil, puis ouvrir son ancienne URL.

À vérifier : `DELETE /games/{id}` répond `204`, la ligne disparaît, et l'ancienne URL affiche
`Cette partie n'existe plus sur le serveur.`

## 9. Les deux erreurs gRPC

**Partie introuvable.** Arrêter l'API avec `Ctrl+C`, vérifier que le port est libre, la relancer,
puis recharger une page de partie.

À vérifier : le message `Cette partie n'existe plus sur le serveur. Crée une nouvelle partie.`,
avec le bouton **Nouvelle partie**. Onglet Réseau : `grpc-status: 5` dans les en-têtes de réponse,
pour un `POST` en `200`.

**Identifiant invalide.** Ouvrir `http://localhost:5274/partie/pas-un-guid`.

À vérifier : `Identifiant refusé : L'identifiant de la partie n'est pas valide.` Onglet Réseau :
`grpc-status: 3`.

## 10. Fin de partie

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

À vérifier : la carte de fin annonce le gagnant, avec **nombre de tirs**, **touches**,
**précision** et **durée**. Cliquer sur une case de la grille adverse affiche alors
`La partie est terminée.`

Dernier essai lancé pendant l'écriture de cette procédure : partie terminée après 51 tirs du
joueur, gagnant `Computer`.

## 11. Nouvelle partie

Cliquer sur **Nouvelle partie**.

À vérifier : l'URL change d'identifiant, la phase revient à `Setup`, ma grille est vide et le port
contient de nouveau les cinq navires, et l'ancienne partie reste accessible par son ancienne URL
tant que l'API tourne.
