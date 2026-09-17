# Vérification manuelle du front

Le front n'est couvert par aucun test automatisé (choix assumé, voir les limites du README).
Cette procédure le remplace : elle se déroule à la main, dans le navigateur, et prend quelques
minutes.

Rien de ce qui suit n'a été exécuté dans un navigateur par l'IA : elle n'en a pas. Les attendus
décrits ici viennent du code et des réponses du serveur, vérifiées séparément avec curl et par les
tests. Seul le script de la section 11 a réellement été lancé, contre l'API.

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

- le bouton **Nouvelle partie** ;
- la liste des parties, vide au premier lancement ;
- dans la barre latérale, les cases **Sons** et **Musique**, cochées ;
- une nappe musicale calme démarre au premier clic — les navigateurs interdisent tout son avant
  un geste de l'utilisateur, donc elle ne peut pas partir avant ;
- onglet Réseau : `GET /games` en `200`.

## 2. Créer une partie et poser sa flotte

1. Cliquer sur **Nouvelle partie**.
2. L'URL devient `/partie/{identifiant}`.

À vérifier :

- **ma grille est vide** : c'est au joueur de poser sa flotte ;
- le **port** dessine les navires restants à leur vraie longueur : 5, 4, 3, 3 et 2 cases ;
- le bandeau annonce **Prépare ta flotte** ;
- le sélecteur **Niveau de l'ordinateur** est dans la barre de préparation, réglé sur **Normal** ;
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

## 4. Le niveau de l'ordinateur

1. Choisir **Difficile** dans la barre de préparation.

À vérifier : `POST /games/{id}/difficulty` répond `200`, et la version de la partie augmente.

2. Appuyer sur `F5`.

À vérifier : le niveau affiché est toujours **Difficile**. Il est retenu par le serveur, pas par
l'écran.

3. Après avoir cliqué sur **Commencer**, le sélecteur disparaît avec la barre de préparation. Le
   niveau ne se change plus : une demande à ce moment-là serait refusée en `409 NotInSetup`.

## 5. Placement aléatoire et réinitialisation

- **Placement aléatoire** : les cinq navires apparaissent d'un coup, le port affiche
  `Flotte complète.`, et **Commencer** devient actif.
- **Réinitialiser** : la grille se vide et les cinq longueurs reviennent au port.

## 6. Démarrer la partie

Cliquer sur **Commencer**.

À vérifier : la phase passe à `InProgress`, le port et la barre de préparation disparaissent, et
si l'ordinateur a commencé, ses tirs d'ouverture sont visibles sur ma grille.

Puis cliquer sur une case de **ma** grille : rien ne se passe. Un placement après le démarrage
serait refusé avec `NotInSetup`, mais l'écran n'envoie même plus la demande.

## 7. Tirer

Cliquer sur une case de la **grille adverse**.

À vérifier :

- le message annonce `À l'eau.`, `Touché.` ou `Coulé : …` ;
- les trois résultats se distinguent à l'œil : un raté est une pastille sur l'eau, une case
  touchée est orange, un navire coulé passe entièrement en rouge sombre, y compris sur ma grille ;
- après un tir à l'eau, le message liste les tirs de l'ordinateur ;
- après un tir touché, c'est encore mon tour ;
- onglet Réseau : `POST /games/{id}/shots` en `200`.

**Audio** : les réglages sont dans la **barre latérale**, donc accessibles depuis toutes les pages.
Décocher **Sons** puis tirer — aucun effet sonore. Recocher, le son revient.

**Les deux ambiances** : au moment du clic sur **Commencer**, la nappe calme laisse place à un
thème rythmé — batterie, basse et motif de croches. De retour à l'accueil, la nappe calme revient.
La transition doit se faire sans coupure ni silence.

## 8. Un tir refusé ne change rien

Cliquer **deux fois** sur la même case de la grille adverse.

À vérifier :

- le second clic affiche `Cette case a déjà été tirée.` ;
- la grille n'a pas changé : aucune case nouvelle, aucun tir de l'ordinateur, la phase et le tour
  sont identiques ;
- onglet Réseau : le second `POST … /shots` répond `409`, corps `"rejection": "AlreadyTargeted"`.

Le front n'anticipe rien : tant que le serveur n'a pas répondu, aucune case ne bouge.

## 9. Reprise, historique et suppression

1. Appuyer sur `F5` sur `/partie/{identifiant}` : la partie revient dans le même état, tirs
   compris. L'identifiant vient de l'URL, et l'état est relu par gRPC-Web.
2. Revenir à l'accueil : la partie apparaît dans la liste, avec son état, sa date, son nombre de
   tirs et sa durée. **Reprendre** rouvre son URL.
3. Supprimer une partie depuis l'accueil, puis ouvrir son ancienne URL.

À vérifier : `DELETE /games/{id}` répond `204`, la ligne disparaît, et l'ancienne URL affiche
`Cette partie n'existe plus sur le serveur.`

## 10. Les deux erreurs gRPC

**Partie introuvable.** Arrêter l'API avec `Ctrl+C`, vérifier que le port est libre, la relancer,
puis recharger une page de partie.

À vérifier : le message `Cette partie n'existe plus sur le serveur. Crée une nouvelle partie.`,
avec le bouton **Nouvelle partie**. Onglet Réseau : `grpc-status: 5` dans les en-têtes de réponse,
pour un `POST` en `200`.

**Identifiant invalide.** Ouvrir `http://localhost:5274/partie/pas-un-guid`.

À vérifier : `Identifiant refusé : L'identifiant de la partie n'est pas valide.` Onglet Réseau :
`grpc-status: 3`.

## 11. Fin de partie

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
**précision** et **durée**, **par-dessus** les deux grilles dans leur état final — la mise en page
ne se décale pas, la carte flotte au milieu de l'écran, fond légèrement assombri. Cliquer sur une
case de la grille adverse affiche alors `La partie est terminée.`

Dernier essai lancé pendant l'écriture de cette procédure : partie terminée après 51 tirs du
joueur, gagnant `Computer`.

## 12. Le panneau de règles

Cliquer sur le bouton **?**, en bas à droite.

À vérifier :

- le panneau s'ouvre **centré sur la fenêtre**, pas sur la zone de contenu, et recouvre aussi la
  barre latérale ;
- s'il est plus haut que l'écran, il défile tout seul sans que la page bouge ;
- la croix et un clic à côté le ferment ;
- le bouton **?** est bien dans le coin de la fenêtre, à toutes les phases de la partie.

## 13. Nouvelle partie

Cliquer sur **Nouvelle partie**.

À vérifier : l'URL change d'identifiant, la phase revient à `Setup`, ma grille est vide et le port
contient de nouveau les cinq navires, et l'ancienne partie reste accessible par son ancienne URL
tant que l'API tourne.

## 14. Attaque spéciale

Sans avoir touché à **Activer les attaques spéciales** dans le formulaire de partie personnalisée
(coché par défaut), la règle est active. Dès que la partie est `InProgress`, deux badges ronds
apparaissent : un en bas à gauche de la grille adverse (ma jauge), un en bas à gauche de ma grille
(celle de l'ordinateur) — chacun avec un symbole ✦ et cinq points.

1. Tirer sur la grille adverse, à l'eau ou touché peu importe, jusqu'à ce que les cinq points du
   badge soient remplis. Compter uniquement mes propres tirs acceptés ; les tirs de l'ordinateur ne
   font pas avancer ma jauge.

À vérifier :

- les points se remplissent un par un, un tir accepté à la fois ;
- tant qu'elle n'est pas pleine, le symbole ✦ reste **grisé** ;
- une fois les cinq points remplis, le symbole passe à l'**orange** et la grille adverse prend un
  **contour pointillé orange** — sans avoir rien cliqué d'autre que des tirs normaux.

2. Survoler une case de la grille adverse, jauge pleine.

À vérifier : une **croix de 5 cases** (la case et ses 4 voisines par les côtés) se surligne en
vert ; près d'un bord, seules les voisines qui existent réellement s'allument, et une case déjà
tirée dans la croix ne s'allume pas non plus.

3. Cliquer sur une case pour tirer.

À vérifier :

- **aucun bouton à cliquer avant** : le tir déclenche directement l'attaque spéciale ;
- le message commence par `Attaque spéciale !` ;
- jusqu'à 5 cases se révèlent sur la grille adverse (celles qui existaient et n'avaient pas déjà
  été tirées) ;
- le contour pointillé disparaît, les cinq points reviennent à vide et le symbole regrise : la
  jauge se réinitialise que l'attaque touche ou non ;
- onglet Réseau : `POST /games/{id}/shots` en `200`, corps de requête avec `"specialAttack": true`.

4. Observer le badge de l'ordinateur au fil de la partie.

À vérifier : ses points se remplissent à chacun de ses tirs, le symbole passe à l'orange une fois
les cinq remplis, puis tout retombe à vide quand l'ordinateur l'utilise — visible aux tours
suivants, sans qu'aucune action ne soit requise côté joueur.

5. Décocher **Activer les attaques spéciales** en créant une partie personnalisée, puis jouer.

À vérifier : aucun badge n'apparaît sur aucune des deux grilles, et la section « Attaque spéciale »
du panneau de règles (`?`) n'apparaît pas non plus.

Vérifié en vrai contre l'API (pas le navigateur, l'IA n'en a pas) : une charge à 5 tirs suivie
d'une attaque spéciale répond `200`, résout la croix demandée et remet la jauge à 0 — voir
`api.http`, section attaque spéciale. Le réglage désactivé est couvert par les tests automatisés
(`SpecialAttackEndpointTests`), pas rejoué ici à la main.
