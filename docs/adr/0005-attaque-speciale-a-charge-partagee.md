# ADR 0005 : attaque spéciale à charge partagée

## Statut et date
Proposé — 2026-09-17.

## Contexte
Demande du binôme : ajouter des attaques spéciales qui « se chargent avec le temps », en plus
des corrections de bugs d'affichage traitées séparément. Contrairement à ces corrections, il
s'agit d'une nouvelle règle de jeu — elle touche l'autorité du serveur, la symétrie entre le
joueur et l'ordinateur, et le contrat de tir existant. Trois questions structurantes ont donc été
posées au binôme avant tout code :

1. **Mécanisme de charge** — un tour d'attente accumulé plutôt qu'un déclenchement aléatoire ou
   lié à un score. Choisi : un tour d'attente = une charge.
2. **Forme de l'effet** — une attaque qui touche plusieurs cases d'un coup plutôt qu'une case
   unique renforcée (dégâts accrus, contournement d'un refus, etc.). Choisi : plusieurs cases.
3. **Symétrie avec l'ordinateur** — l'ordinateur soumis exactement aux mêmes règles, cohérent
   avec l'invariant déjà en place pour le tir simple (`docs/REGLES.md`, section L'ordinateur).
   Choisi : oui, mêmes règles.

Ces trois réponses laissaient encore deux choix d'implémentation sans réponse du binôme : le
nombre de tirs acceptés nécessaires pour charger, et la forme exacte des « plusieurs cases ». Les
deux ont été tranchés unilatéralement (voir Options), signalés comme corrigibles, et le binôme ne
les a pas contredits en validant le pas 1.

Contraintes qui pèsent sur le choix :
- Invariant n°1 : la position d'un navire adverse non coulé ne doit jamais fuiter. Une attaque
  qui révèle plusieurs cases d'un coup ne doit pas en révéler plus que ce que chaque case aurait
  révélé individuellement.
- `Board.ReceiveShot` renvoie déjà un refus sans effet de bord (`OutOfBounds`, `AlreadyTargeted`) :
  une attaque à plusieurs cases doit pouvoir réutiliser cette propriété plutôt que de dupliquer la
  logique de validation case par case.
- `PlacementRules.SideNeighbours` calcule déjà les 4 voisines par les côtés d'une case, pour la
  règle de contact au placement — même geste géométrique qu'une croix de tir.
- Le contrat de tir existant (`FireRequest`, route `POST /games/{id}/shots`) est déjà exercé par
  `api.http`, les tests d'API et le front ; toute évolution doit rester compatible avec un client
  qui ignore le nouveau champ.
- `ShotHistoryDto` porte un unique résultat par entrée d'historique ; en changer la forme aurait
  une conséquence sur l'affichage de l'historique récent, déjà en place.

## Options envisagées

### Nombre de tirs acceptés par charge
Le binôme a choisi « un tour d'attente = une charge » sans préciser combien. Choisi : **5 tirs
acceptés de ce camp**, arbitraire, signalé comme corrigible au moment de la validation du pas 1
— aucune valeur ronde ne s'imposait sans donnée de partie réelle pour la calibrer.

### Empilement des charges
- **Plafonné à une charge** (choisi) : une fois chargée, la jauge n'avance plus tant que la
  charge n'est pas utilisée. Une seule règle de déclenchement à tester, pas de file d'attente.
- **Empilable** : plusieurs charges disponibles, attaque spéciale utilisable plusieurs tours de
  suite. Écarté : complexifie le calcul de jauge et n'a pas été demandé.

### Forme des « plusieurs cases »
- **Croix de 5 cases** (choisi) : la case visée et ses 4 voisines par les côtés. Réutilise
  `PlacementRules.SideNeighbours`, déjà écrit pour la règle de contact au placement, et reste
  cohérent avec cette définition du voisinage (les côtés, pas les diagonales) déjà connue du
  binôme.
- **Carré 3×3** (9 cases) : voisinage plus large, mais sans primitive existante à réutiliser et
  sans justification supplémentaire pour la taille.

### Contrat réseau
- **Champ optionnel sur la route de tir existante** (choisi) : `FireRequest.SpecialAttack`
  (`bool`, par défaut `false`). Aucune nouvelle route, un ancien client qui omet le champ obtient
  le comportement d'avant, vérifié par la suite de tests complète.
- **Route dédiée** (`POST /games/{id}/shots/special`) : deux routes à maintenir en cohérence
  (version attendue, motifs de refus communs) pour une différence d'un seul champ.

### Agrégation dans les statistiques et l'historique
- **Un seul tir, résultat agrégé** (choisi) : `AreaShotResult.AggregateOutcome` retient le
  meilleur résultat parmi les cases résolues (coulé > touché > à l'eau). `ShotHistoryDto` ne
  change pas de forme.
- **Une entrée d'historique par case résolue** : plus fidèle au détail, mais change le contrat
  existant et le calcul de « nombre de tirs » (`Statistics.TotalShots`), qui compterait alors une
  attaque spéciale comme plusieurs tirs.

### Traitement des voisines invalides de la croix
- **Ignorées silencieusement** (choisi) : une voisine hors grille ou déjà tirée est retirée de la
  liste des cases résolues sans provoquer de refus. Seule la case centrale est soumise aux motifs
  de refus habituels du tir.
- **Attaque refusée dans son ensemble** dès qu'une voisine est invalide : aurait rendu l'attaque
  spéciale inutilisable près des bords ou une fois la zone déjà bien tirée, sans bénéfice évident.

## Décision
On a fait le choix de bloquer seulement le tir si la case centrale et de laisser les autres cases faire une erreur silencieuse. Comme ça cela simplifie la lique de ce nouveau type de tir.

## Conséquences
Constatées pendant l'implémentation (commits en Références) :

- **Moteur (pas 1)** : `GameRules.SpecialAttackChargeInterval` = 5 ; `Game` porte deux compteurs
  privés (`playerSpecialAttackProgress`, `computerSpecialAttackProgress`), une méthode
  `PlayerFireSpecialAttack` et une méthode interne `FireArea` partagée avec l'IA, qui résout la
  case centrale puis `PlacementRules.SideNeighbours(centre)` via `Board.ReceiveShot` — la même
  méthode que le tir simple, donc les mêmes garanties de non-effet sur refus. `ShotRejection`
  gagne `SpecialAttackNotCharged`. Une logique de rejeu et de victoire dupliquée entre le tir
  simple et l'attaque de zone a été extraite dans `ResolveTurnAfterShots`, partagée par les deux.
  Un filtre défensif (`.Where(result => result.IsAccepted)`) s'est révélé mort par mutation
  (les deux appelants garantissent déjà des résultats acceptés) et a été supprimé plutôt que
  protégé par un test.
- **Stockage et endpoint (pas 2)** : `StoredGame.TryFireSpecialAttack` reprend le tracé
  optimiste-concurrent de `TryFire` (version attendue, historique). L'endpoint existant
  `POST /games/{id}/shots` branche sur `FireRequest.SpecialAttack` ; `FireAreaLocked` reproduit
  la reclassification déjà en place pour `FireLocked` (l'`OutOfBounds` de la case centrale devient
  un `400` sur la clé `target`, les autres refus restent des `409`).
- **gRPC-Web (pas 3)** : `GameState` (proto) gagne `special_attack_charge_interval`,
  `player_special_attack_progress`, `computer_special_attack_progress`, valeurs brutes plutôt
  qu'un texte déjà mis en forme — le client compare lui-même à l'intervalle. Un test de câblage
  n'a d'abord pas détecté l'oubli de la jauge adverse dans le mapper (mutation : 6/6 tests gRPC
  toujours verts) ; resserré en s'appuyant sur une graine déjà documentée
  (`GameEndpointsTests.Seed = 20260915`, qui garantit un premier tour de l'ordinateur), la même
  mutation a ensuite fait échouer le test.
- **Front (pas 4)** : jauge affichée dans le panneau État pour les deux camps ; bouton dédié
  au-dessus de la grille adverse, actif seulement une fois chargé, qui arme un mode « attaque
  spéciale » consommé (ou abandonné) au tir suivant, jamais laissé actif entre deux parties de
  l'écran. L'aperçu de la croix au survol reproduit côté client la même exclusion des voisines
  hors grille ou déjà tirées que le serveur applique, pour ne pas promettre une case que le
  serveur ignorera de toute façon — mais reste un aperçu, sans aucune décision : le serveur reste
  seul juge. Aucune donnée nouvelle n'est nécessaire pour l'affichage du résultat final : l'état
  complet renvoyé après le tir suffit à peindre les cases de la croix, la seule limite étant
  qu'aucune n'est individuellement animée par un flash d'impact — l'agrégat renvoyé par le
  contrat ne porte qu'un résultat, pas un résultat par case.
- **Vérification en vrai (pas 5)** : la charge complète (5 tirs) suivie d'une attaque spéciale a
  été rejouée deux fois contre une instance isolée de l'API (curl/Python, pas le navigateur), sur
  un port dédié pour ne pas perturber les serveurs de développement du binôme. Une fois avec un
  touché dans la croix (la main reste au joueur), une fois entièrement à l'eau — dans les deux
  cas `200`, jauge revenue à `0`, comptée comme un seul tir dans les statistiques. `api.http`
  reprend cette même séquence de requêtes chaînées.
- **Suite de tests** : 607 tests passent à la fin du pas 3. `docs/REGLES.md` et
  `docs/VERIFICATION-MANUELLE.md` mis à jour en pas 5.

## Vérification et réexamen
Ont à fait des test dans le front-end et en vérifier les logs pour les crashs silencieux.

## Références
- Règles : `docs/REGLES.md`, section Attaque spéciale. Procédure manuelle :
  `docs/VERIFICATION-MANUELLE.md`, section 14.
- Contrat : `api.http` (section attaque spéciale), `Protos/battleship.proto`.
- Commits :
  - `039e5dc` ajoute l attaque speciale a charge partagee par camp
  - `21e5c2c` expose l attaque speciale sur la route de tir existante
  - `f5079ca` expose la jauge d attaque speciale en grpc-web
- Vérifications : `dotnet test BattleShip.slnx` → 607 réussis sur 607 à la fin du pas 3 ; deux
  mutations exécutées puis annulées pendant cette extension — filtre mort dans
  `ResolveTurnAfterShots` (**0 échec**, code supprimé plutôt que protégé) et jauge adverse omise
  du mapper gRPC (**0 échec avant resserrement de l'assertion, 1 échec après**) ; `api.http` rejoué
  avec curl/Python contre une instance isolée de l'API.
