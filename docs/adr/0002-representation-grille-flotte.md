# ADR 0002 : représentation de la grille et de la flotte dans le moteur

## Statut et date
Accepté — 2026-09-15.

## Contexte
Phase moteur du socle (S1 à S3 du backlog, plus la vue révélée) : grille 10×10, flotte par
défaut (5, 4, 3, 3, 2), placement aléatoire, tirs, tours, victoire. Règles de référence :
`docs/REGLES.md`.

Contraintes qui pèsent sur la représentation :
- `BattleShip.Models` ne référence aucun autre projet et n'a aucune dépendance à ASP.NET, JSON
  ou gRPC. Il est référencé par `BattleShip.App`, donc livré au navigateur.
- Le moteur doit être testable sans serveur ; `BattleShip.Tests` référence `BattleShip.API`.
- Invariant n°1 du projet : la position d'un navire non découvert ne sort jamais du serveur.
- La solution est limitée à 4 projets par le sujet.
- Pas d'abstraction introduite « au cas où » ; un argument tiré d'une extension du backlog
  (E0 grille configurable, E2 niveaux d'IA, E3 historique) est recevable.

## Options envisagées

### Option A — grille de cases
Une matrice `largeur × hauteur` ; chaque case sait si elle a été tirée et quel navire l'occupe.
- Hors grille et case déjà tirée : lecture directe de la case.
- Contact par les côtés : lecture des 4 cases voisines dans la matrice.
- Limite — « coulé » : la case doit pointer vers son navire et chaque navire tenir un compteur
  de touches ; la position d'un navire existe alors à deux endroits qui peuvent diverger.
- Limite — masquage : la vue se construit case par case et lit le navire de chaque case, y
  compris des cases jamais tirées ; une condition fausse suffit à révéler une position.
- Sérialisation : la vue a la forme de l'écran, mais `System.Text.Json` ne sérialise pas
  `T[,]` (il faudrait des tableaux de tableaux).
- Tests : préparer un scénario demande un outil qui remplit les cases.
- E0 : la matrice est dimensionnée à la construction, sans difficulté particulière.

### Option B — liste de navires et ensemble des tirs
Largeur, hauteur, navires (chacun défini par ses cases), ensemble des cases tirées.
- Hors grille : comparaison avec les dimensions. Case déjà tirée : recherche dans l'ensemble.
- « Coulé » : toutes les cases du navire sont dans les tirs ; une seule source de vérité.
- Masquage : la vue énumère ce qui est révélé (tirs, navires coulés) au lieu de filtrer les
  navires.
- Sérialisation : listes de coordonnées numériques, conformes à la règle « jamais de B7 sur
  le réseau ».
- Tests : un navire se déclare directement par ses coordonnées.
- E0 : les dimensions ne sont que deux entiers. E3 : l'ensemble de tirs devient une liste
  ordonnée.
- Limite — contact par les côtés : il faut calculer les cases occupées puis tester leurs
  voisines, un peu plus de code qu'en A.
- Limite — affichage : le front doit reconstruire une grille à partir des listes.

## Décision
Option B retenue. La position d'un navire n'existe qu'à un seul endroit, et la vue envoyée au joueur se construit à partir du journal des réponses sans jamais lire la flotte — une erreur dans la vue ne peut donc pas révéler un navire jamais touché. Le moteur vit dans BattleShip.API.Engine et non dans Models, parce que Models est livré au navigateur : un type contenant les positions y serait désérialisable côté client.

## Conséquences
Constatées pendant l'implémentation (commits de la phase, voir Références) :

- **Le moteur vit dans `BattleShip.API`, espace de noms `BattleShip.API.Engine`.**
  `BattleShip.Models` ne contient que le vocabulaire sans information cachée : `Coordinate`,
  `Side`, `ShotOutcome`, `ShotRejection`, `GamePhase`.
- **La frontière du moteur est tenue par convention, pas par le compilateur.** Le SDK Web de
  `BattleShip.API` active des `using` implicites vers les espaces de noms ASP.NET : rien
  n'empêche un fichier de `Engine/` de s'en servir. Un projet séparé rendrait la frontière
  vérifiable, mais la solution est limitée à 4 projets.
- **Des types contenant la position des navires restent publics** : `Board.Ships`,
  `Game.PlayerBoard` et `Game.ComputerBoard`. Ils sont utilisés par les tests de placement
  et de symétrie. L'invariant n°1 dépend donc de ce que le transport (S6, S8) choisit
  d'exposer, jusqu'au test sur le JSON brut prévu en S7.
- **L'ensemble des tirs est devenu un journal des réponses** (`d3dd499`).
  `Board` garde, pour chaque case tirée, le `ShotResult` annoncé au tireur. `Reveal()`
  construit la vue uniquement à partir de ce journal, sans lire la flotte, et renvoie une
  copie figée. Le tir est enregistré en dernier, après tous les refus. La mutation qui
  l'enregistre avant le refus « hors grille » passait les 381 tests existants ; deux tests
  ont été ajoutés et échouent contre elle.
- **Le placement reçoit dimensions et longueurs en paramètres dès le socle.** C'est ce qui
  rend testable la limite de 1000 essais, la flotte par défaut se plaçant toujours en 10×10.
- **La vue révélée porte les dimensions de la grille** (`Width`, `Height`) depuis `459b9e5` :
  la stratégie de l'ordinateur en a besoin pour ne viser que des cases de la grille.

## Vérification et réexamen
La représentation a évolué en cours d'implémentation : l'ensemble des tirs est devenu un journal des réponses, parce que la première version obligeait encore à raisonner sur la flotte pour construire la vue. Les mutations du masquage (26 puis 25 tests en échec) confirment que la vue est protégée. Nous reverrions ce choix si des états de case autres que eau/navire apparaissaient.

## Références
- Règles : `docs/REGLES.md`.
- Commits de la phase :
  - `a39a7d3` ajoute le vocabulaire partage du moteur dans models
  - `a12fabd` ajoute la reception d un tir sur une grille
  - `688be41` ajoute le placement aleatoire de la flotte
  - `4db6565` ajoute le deroulement d une partie et la reponse de l ordinateur
  - `5117a53` renforce le test du second demarrage d une partie
  - `79cb391` signale la boucle sans fin possible du tour de l ordinateur
  - `d3dd499` remplace l ensemble des tirs par un journal et masque la vue
