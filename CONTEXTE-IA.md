# Contexte du projet

## Contraintes du cours

- .NET 10 stable ; vérifier global.json et dotnet --version.
- API ASP.NET Core Minimal API, Blazor WebAssembly, modèles partagés, tests.
- FluentValidation sur les entrées serveur ; au moins un échange gRPC-Web fonctionnel depuis le navigateur.
- Règles vérifiées côté serveur ; informations adverses cachées préservées.
- Projet attendu au-delà du socle : ambition, pertinence et qualité des extensions évaluées.
- Résultat compris et défendable par chaque membre du binôme.

## À compléter par le binôme

- Vision du projet, règles et expérience visée :

  On veut une partie qui se comprend sans explication : deux grilles cote a cote, un message
  clair a chaque coup, et quand un tir est refuse, le joueur sait pourquoi. L'adversaire doit
  donner l'impression de chercher, pas de tirer au hasard.

  Règles détaillées : `docs/REGLES.md`.
- Backlog, priorités et périmètre retenu (dans l'ordre) :
  - Socle strict (grille 10×10, flotte par défaut, placement aléatoire) — **livré, S0 à S10** :
    - S0 — Environnement : `dotnet --version`, `global.json`, build, ports — risque : SDK différent chez le binôme
    - S1 — Modèle du domaine : coordonnée, navire, grille, flotte, résultat de tir — risque : modèle partagé qui fuit vers le front
    - S2 — Placement aléatoire : débordement, chevauchement, contact par les côtés, limite de 1000 essais — risque : off-by-one sur les bornes
    - S3 — Moteur de partie : états, tirage au sort au démarrage, rejeu tant qu'on touche, refus, victoire unique — risque : l'ordinateur qui joue après un refus
    - S4 — IA chasse-cible via la même méthode de tir que le joueur — risque : IA qui vise une case déjà tirée
    - S5 — Stockage en mémoire, accès concurrents, partie introuvable — risque : dictionnaire non protégé
    - S6 — Minimal API (créer, placer, démarrer, tirer, lire l'état), FluentValidation, `api.http` — risque : mauvais choix entre ValidationProblem, NotFound et Conflict
    - S7 — Test de masquage sur le JSON brut de l'état — risque : tester l'objet typé au lieu de la chaîne
    - S8 — gRPC-Web depuis le navigateur : lecture d'état pour la reprise, erreur « partie introuvable » — risque : CORS et contrat à garder hors de `Models`
    - S9 — Front Blazor : deux grilles, messages de coup et de refus, écran de fin, reprise — risque : règles dupliquées côté client
    - S10 — Livrables au fil de l'eau (README, ADR, revues IA, PROMPTS) — risque : reconstitués en fin de projet
  - Extensions :
    - État au 17/09 : E0bis, E1, E2 et E3 sont livrées ; E0 et E4 ne sont pas faites.
    - E0 — **non réalisée** — Configuration : grille rectangulaire de 5 à 15, flotte de 0 à 3 navires par taille — risque : validation qui dépend des dimensions, refus d'une configuration impossible
    - E0bis — **livrée** — Placement manuel et placement aléatoire re-tirable. Le geste final n'est pas « clic + rotation » : cliquer un navire posé le reprend en main, la molette l'oriente, un clic le repose. Voir `docs/adr/0004`.
    - E1 — **livrée** — Statistiques de partie : tirs, touches, ratés, précision, durée, historique des tirs.
    - E2 — **livrée** — Niveaux d'IA : aléatoire (Facile), chasse-cible (Normal), probabiliste (Difficile), choisis pendant la préparation. La mesure sur 1000 parties n'existe que pour la chasse-cible.
    - E3 — **livrée en partie** — Historique des coups et liste des parties reprenables. Le rejeu d'une partie terminée n'est pas fait.
    - E4 — **non réalisée** — Expiration des parties abandonnées — risque : conflit avec la reprise
  - Écarté :
    - Multijoueur : On prefere un jeu contre l'ordinateur qui marche vraiment de bout en bout plutot qu'un
      multijoueur a moitie fini. Il faudrait gerer l'attente de l'adversaire et la
      synchronisation des deux sessions, ce n'est pas notre priorite.
    - Persistance en base : la reprise fonctionne en mémoire et le redémarrage du serveur est traité par « partie introuvable ».
    - Placement par glisser-déposer : il demande de l'interop JavaScript, alors que clic + rotation couvre la même règle.
    - Flux gRPC en continu : sans multijoueur, il n'y a aucun événement à pousser.
    - Flotte différente pour l'ordinateur : contraire à la règle de symétrie.
    - Comptes utilisateurs, déploiement, classement : hors des critères techniques.
    - Animations et sons : **écartés au cadrage, finalement réalisés** (animations de tir, effets sonores,
      deux ambiances musicales). Justification de ce revirement à écrire par le binôme.
- Organisation du code et contrats :
  - Solution `BattleShip.slnx`, 4 projets, tous en `net10.0` avec nullable activé.
  - `BattleShip.API` (Minimal API) → référence `BattleShip.Models`.
  - `BattleShip.App` (Blazor WebAssembly) → référence `BattleShip.Models`.
  - `BattleShip.Models` → aucune référence de projet, aucun package.
  - `BattleShip.Tests` (xUnit) → référence `BattleShip.API`.
  - `BattleShip.Tests` référence aussi `BattleShip.App` avec `Aliases="app"`, pour tester les
    règles d'écran sans bUnit. Sans cet alias, les types générés depuis `battleship.proto`
    existeraient deux fois et le projet ne compilerait plus (`CS0433`).
  - Règles du jeu : `docs/REGLES.md`. Contrat HTTP : `api.http`, 14 routes sous `/games`.
    Contrat gRPC-Web : `Protos/battleship.proto`, opération `GetGame`. L'endpoint du gabarit
    `/weatherforecast` a été supprimé.
- Commandes, ports et environnement :
  - Dépôt : https://github.com/LaChevrenen/Battleship-ANDRADE-CARLINO
  - SDK : `global.json` à la racine (`10.0.100`, `rollForward: latestFeature`) ;
    `dotnet --version` → `10.0.401`.
  - `dotnet build BattleShip.slnx` → réussite, 0 avertissement, 0 erreur.
  - `dotnet test BattleShip.slnx` → 568 réussis sur 568 (dernière mesure : 17/09, après le
    déplacement du niveau de l'ordinateur).
  - Ports (`Properties/launchSettings.json`) :
    - API : `http://localhost:5062`, `https://localhost:7260`
    - App : `http://localhost:5274`, `https://localhost:7172`
- Conventions et méthode de collaboration :

  Julien code sur sa machine, c'est lui qui a Claude Code installe. Romain ecrit les tests,
  et on reflechit ensemble aux choix techniques avant de lancer quoi que ce soit : les
  prompts sont prepares a deux. On relit les changements ensemble avant de les integrer.
  Depot commun : github.com/LaChevrenen/Battleship-ANDRADE-CARLINO.

  - Commits : une ligne, en français, impératif présent, minuscule, 72 caractères au plus,
    sans point final. Pas d'emoji, de trailer ni de mention d'outil. Un commit = une intention.
  - Code : C# 14, `sealed` par défaut, `record` pour les DTO, pas de logique métier dans
    `Program.cs`, noms de tests en français (`Un_coup_hors_grille_est_refuse`).
- Décisions structurantes et références des ADR :
  - `docs/adr/0001-modele.md` — gabarit fourni par le cours, non modifié.
  - `docs/adr/0002-representation-grille-flotte.md` — représentation de la grille et de la flotte.
  - `docs/adr/0003-operation-exposee-en-grpc-web.md` — opération exposée en gRPC-Web.
  - `docs/adr/0004-placement-manuel-de-la-flotte.md` — placement manuel, rotation et reprise en main.
  - Manquants : aucun ADR sur les niveaux d'IA, ni sur les statistiques et l'historique.
- Vérifications réalisées et limites connues :
  - Tirs pour couler la flotte par défaut en 10×10, sur 1000 flottes aléatoires : chasse-cible
    moyenne 61,3 (min 27, max 100) ; tir aléatoire pur moyenne 95,5 (min 64, max 100).
  - Le seuil « moyenne chasse-cible < 75 » est protégé par le test
    `La_chasse_cible_coule_une_flotte_nettement_plus_vite_que_le_hasard` (commit `6e7baed`).
  - L'historique des parties est exposé par `GET /games`. Les résumés portent l'identifiant, la
    date de création, la phase, le gagnant éventuel, le nombre de tirs du joueur et la durée.
    Aucune position de navire n'y figure.
  - Une partie se supprime par `DELETE /games/{id}`.
  - Une partie créée apparaît en phase `Setup` et peut être reprise par son URL ; test API
    `Une_partie_creee_apparait_dans_l_historique_et_reste_reprenable`.
- Arbitrages et évolution du périmètre :

  Le perimetre a ete revu au moment du cadrage : la grille et la flotte configurables, ainsi
  que le placement manuel, etaient d'abord prevus dans le socle. On les a sortis en
  extensions pour garantir une partie jouable de bout en bout au plus vite. Le socle se joue
  en 10x10 avec placement aleatoire.
