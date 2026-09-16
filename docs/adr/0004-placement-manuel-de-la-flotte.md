# ADR 0004 : placement manuel de la flotte

## Statut et date
Proposé — 2026-09-16.

## Contexte
`docs/REGLES.md` prévoit que le joueur dispose lui-même ses navires, et qu'il peut aussi les faire
tirer au hasard, autant de fois qu'il veut, tant que la partie n'a pas commencé. Le socle ne
plaçait les deux flottes qu'au hasard, une seule fois, à la création de la partie (S1 à S3).

Contraintes qui pèsent sur le choix :
- Toute règle de placement est jugée côté serveur : débordement, chevauchement, contact par un
  côté, et composition de la flotte. Le client ne décide rien.
- Les règles de validité existaient déjà, écrites une fois dans `RandomFleetPlacer` (ADR 0002).
  Un second appelant ne doit pas les dupliquer.
- `Board` est immuable et reçoit sa flotte à la construction ; c'est ce qui garantit aujourd'hui
  qu'une grille ne change plus une fois la partie démarrée.
- Les opérations qui modifient une partie passent par `GameStore`, sous un verrou par partie et
  avec une version attendue (ADR 0002, S5).
- Pas d'interop JavaScript côté front : l'interaction doit tenir avec les événements que Blazor
  gère nativement.

## Options envisagées

### Option A — sélection, orientation, clic sur l'origine
- Le joueur choisit une longueur, bascule l'orientation, survole une case et clique pour poser.
  L'origine, la longueur et l'orientation partent au serveur, qui accepte ou refuse.
- Un aperçu au survol est possible, puisque le client connaît la forme du navire qu'il a choisi.
- Limite : le client connaît la géométrie d'un navire (une file de N cases dans une direction).
  C'est une information d'affichage, pas une règle : il ne juge ni le débordement, ni le
  chevauchement, ni le contact.
- Limite : il faut une commande de rotation, donc un geste de plus à rendre visible.

### Option B — clic de début puis clic de fin
- Le joueur clique les deux extrémités ; le serveur déduit la longueur et l'orientation.
- Aucune géométrie côté client : il n'envoie que deux cases.
- Limite : plus de refus à expliquer — cases non alignées, longueur absente de ce qu'il reste à
  poser — pour des gestes que le joueur croit valides.
- Limite : aucun aperçu fidèle avant le second clic.

### Granularité de la validation
- **Navire par navire** : chaque pose est acceptée ou refusée immédiatement, et aucun état de
  placement n'existe côté client.
- **Flotte complète en une fois** : moins d'appels, mais un refus tardif qui doit désigner le
  navire fautif, et une flotte en construction à tenir dans le navigateur.

### Version attendue sur les routes de préparation
- **Oui, comme le tir** : deux onglets ouverts sur la même partie ne peuvent pas poser chacun un
  navire depuis un état différent ; le second reçoit `409 StaleVersion`.
- **Non, pas en préparation** : la préparation serait le seul endroit où une écriture ne dit pas
  depuis quel état elle a été décidée.

### Origines valides calculées par le serveur
Pour colorer l'aperçu en vert ou en rouge, deux façons de savoir si un placement est possible :
- **Prévisualisation à chaque survol** (`POST …/ships/preview`) : des centaines d'appels pendant
  la préparation, et un aperçu en retard sur la souris s'il faut les amortir.
- **Liste des origines valides** (`GET /games/{id}/placements?length=&orientation=`) : une dizaine
  d'appels pour toute la préparation, un par changement de sélection ou d'orientation. Le client
  ne fait qu'une appartenance à une liste que le serveur lui a donnée.
- **Origines valides incluses dans l'état** : aucun appel supplémentaire, mais l'état grossit
  (15 à 20 Ko) et est recalculé à chaque lecture, y compris hors préparation.

## Décision

## Conséquences
Constatées pendant l'implémentation (commits en Références) :

- **Règles extraites une fois** : `BattleShip.API/Engine/PlacementRules.cs` porte la construction
  des cases d'un navire et la validité d'un placement (débordement, chevauchement, contact par un
  côté). `RandomFleetPlacer` ne duplique plus rien : il énumère des positions candidates et
  demande à `PlacementRules` si elles sont valides ; le placement manuel fait le même appel.
  C'est l'extraction annoncée au pas 3 de la phase moteur, quand un second appelant apparaîtrait.
- **Préparation portée par un type distinct** : `FleetUnderConstruction` est le seul objet
  modifiable de la préparation. `Board` n'a pas changé — chaque placement accepté produit une
  nouvelle `Board`.
- **Impossibilité structurelle plutôt que garde** : `Game` lâche la flotte en construction au
  démarrage (le champ passe à `null`), juste après avoir figé la `Board`. Les opérations de
  préparation n'ont alors plus d'objet à modifier et répondent `NotInSetup`. Le test
  `Un_placement_apres_le_demarrage_est_refuse_et_ne_change_pas_la_grille` vérifie les trois
  opérations, puis que `PlayerBoard` est **le même objet** qu'avant (`Assert.Same`).
- **Cascade sur `Game` et les tests existants** : `CreateWithRandomFleets` devient
  `CreateWithRandomComputerFleet`, `TryStart` distingue `AlreadyStarted` de `FleetIncomplete`, et
  10 tests ont échoué au premier passage (9 dans `GameEndpointsTests`, 1 dans
  `GameGrpcServiceTests`), tous parce qu'une partie créée n'a plus de flotte joueur.
- **Coût en durée de la suite** : environ 2,5 s avant l'extraction, 3,8 s après, soit +1,3 s. Le
  placement aléatoire vérifie maintenant chaque position candidate par `PlacementRules`, et le
  test qui joue 1000 parties en paie l'essentiel. La piste d'optimisation — rendre au placeur un
  ensemble de cases bloquées, sans toucher aux règles — est notée en commentaire et n'est pas
  implémentée.
- **Routes** : `POST /games/{id}/ships`, `POST /games/{id}/ships/remove` (qui a remplacé
  `…/ships/undo`, lequel ne défaisait que le dernier navire posé), `POST /games/{id}/fleet/random`,
  toutes avec version attendue ; `GET /games/{id}/placements` pour les origines valides, sans
  version puisqu'elle ne modifie rien. `POST …/ships/undo` avait été préféré à
  `DELETE …/ships/last` parce que `HttpClient.DeleteAsync` n'envoie pas de corps.
- **Motifs de refus** : `PlacementRejection` = `OutOfBounds`, `Overlap`, `AdjacentShip`,
  `LengthNotAvailable`, `NoShipHere`, `NotInSetup` ; `StartRejection` gagne `FleetIncomplete`.
  Après le démarrage, un retrait répond `NotInSetup` et non « aucun navire ici », qui serait faux.
- **Ordre des contrôles** : la disponibilité de la longueur est vérifiée **avant** la géométrie.
  Une demande hors grille sur une longueur déjà posée répond donc `409 LengthNotAvailable` et non
  `400`. Constaté en rejouant `api.http`, qui a été corrigé pour demander une longueur encore
  disponible.
- **Aperçu coloré sans règle côté client** : la page appelle `GET …/placements` à chaque
  changement de sélection ou d'orientation et garde la liste reçue ; `Grid` reçoit les cases de
  l'aperçu et un booléen, et ne calcule aucune validité.

## Vérification et réexamen

## Références
- Règles : `docs/REGLES.md`. Procédure manuelle : `docs/VERIFICATION-MANUELLE.md`.
- Contrat : `api.http`, `Protos/battleship.proto`.
- Commits de l'extension :
  - `a4c83ec` ouvre la preparation de la flotte du joueur cote moteur
  - `18bceeb` ajoute les routes de placement manuel et de tirage aleatoire
  - `e6a4956` ajoute l ecran de preparation avec placement manuel et aleatoire
  - `efdc119` corrige l apercu et documente la verification du placement
  - `6d7a024` ajoute les origines valides et le retrait d un navire par sa case
- Vérifications : `dotnet test BattleShip.slnx` → 529 réussis sur 529 à la fin de l'extension,
  538 après les origines valides ; `api.http` rejoué avec curl contre l'API réelle ; trois
  mutations exécutées puis annulées, sur 529 tests — `PlacementRules` court-circuité par le
  placement manuel (**4 échecs**), version non vérifiée au placement (**1 échec**), flotte non
  lâchée au démarrage (**24 échecs**).
