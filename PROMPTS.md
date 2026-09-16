# Échanges décisifs avec l’IA

## 2026-09-15 — Cadrage des règles du jeu et du périmètre

- Outil / modèle si connu :
  - Claude Code (extension VS Code), modèle Claude Opus 5 : questions de conception,
    spécification, backlog, mise à jour des livrables.
  - Une seconde session Claude, modèle non noté : relecture externe du backlog et des points
    à confirmer.
- Contexte : solution .NET 10 créée, sans code métier. Il fallait trancher les règles du jeu
  et le périmètre avant de commencer S0, et disposer d'une référence commune au binôme.
- Prompt réellement utilisé :
  1. Demande initiale à Claude Code :
     ```text
     Contexte : TP noté .NET 10, bataille navale jouable dans le navigateur, 5 jours en binôme.
     Le socle imposé est dans CLAUDE.md. La solution est créée mais vide de tout code métier.

     Ne produis aucun code pour l'instant.

     1. Pose-moi les questions de conception que tu dois absolument faire trancher avant
        d'écrire une ligne (taille de grille, composition de la flotte, alternance des tours,
        navires adjacents autorisés ou non, révélation d'un navire coulé, rejouer après un
        touché, etc.). Une question à la fois si besoin, avec les conséquences techniques de
        chaque réponse.
     2. Quand j'aurai répondu, écris-moi une spécification de règles d'une page, en français,
        sans jargon .NET : elle doit servir de référence commune à mon binôme et moi.
     3. Puis propose un backlog ordonné : socle d'abord, puis extensions, avec pour chaque
        entrée le risque technique principal et une estimation grossière en heures sur 5 jours.
        Marque clairement ce que tu recommandes de ne PAS faire et pourquoi.

     Rappel : tu ne tranches pas les règles du jeu à ma place.
     ```
  2. Réponses données aux 4 séries de questions à choix de Claude Code :
     - Grille : « 10 x 10 par défaut mais on peut aussi en faire une personnalisée »
     - Flotte : « Composition configurable »
     - Adjacence : « Interdit par les côtés »
     - Après un touché : « Rejoue tant qu'il touche »
     - Bornes de la grille : « Rectangulaire », puis « 5 à 15 chacune »
     - Flotte par défaut : « Classique + choix du nombre », puis « 0 à 3 par taille »
     - Navire coulé : « « Coulé » annoncé »
     - Premier tireur : « Tirage au sort serveur »
     - IA du socle : « Chasse-cible »
     - Case déjà tirée : « Refus explicite »
     - Placement : « Manuel dès le socle mais aussi la possibilité de faire alératoire re tirable
       en gros on peut faire les 2 mais de base les navires c'est a lui de les disposer mais si
       il veut il peut les placer en aléatoire et retirer »
     - Rechargement de page : « Reprise possible »
     - Symétrie : « Oui, identiques »
  3. Relecture externe (seconde session Claude), recopiée dans Claude Code. Il ne s'agit pas
     d'un prompt unique mais d'une conversation : le plan produit par Claude Code y a été
     collé, avec une demande d'avis. Texte de sa réponse :
     ```text
     Point	Mon avis
     Tirage au sort du premier joueur	Au démarrage, pas à la création. Sa déduction est juste : tant que tu peux re-tirer ton placement, la partie n'existe pas vraiment.
     Limite d'essais du placement aléatoire	Oui, borne-la (genre 1000 essais) et écris la limite dans « Limites ». Sans borne tu as une boucle infinie sur une config dense.
     Coordonnées A-O / 1-15	Choix d'affichage pur. Prends-le, c'est lisible, mais garde des indices numériques dans le contrat API — ne fais jamais transiter "B7" sur le réseau.
     Expiration des parties	Tranche-le maintenant en disant non pour le socle. Sinon ça revient hanter S5 et S8.

     59 h de socle + 8 h de tampon sur ~70 h de capacité : il ne reste rien pour les extensions, alors que c'est un critère noté. Or tu as mis dans le socle deux choses que le sujet ne demande pas : la grille/flotte configurable et le placement manuel.

     Ce sont d'excellentes features. Mais assume-les comme des extensions choisies, pas comme du socle — sinon tu te retrouves jeudi soir avec un placement manuel à moitié fini et pas de gRPC.

     Ce que je ferais : socle strict = grille 10×10 + flotte fixe + placement aléatoire uniquement + IA chasse-cible. Jouable de bout en bout fin jour 2. Puis configuration et placement manuel comme E0/E0bis, jour 3-4. Tu perds zéro ambition et tu sécurises le rendu.
     ```
  4. Prompts suivants, non reproduits : mise à jour de `CONTEXTE-IA.md` (réponses du binôme
     recopiées telles quelles) et vérification de `global.json`.
- Réponse et hypothèses résumées :
  - Claude Code a posé 15 questions en 4 séries, avec leurs conséquences techniques et sans
    recommander d'option. Il a rédigé `docs/REGLES.md` à partir des réponses.
  - Il a proposé un backlog : socle S0–S10 estimé à ~59 h, 8 h de tampon et des extensions
    E1–E4, pour une capacité supposée de ~70 h (7 h par jour par personne). La configuration
    et le placement manuel étaient dans le socle. Il a aussi fourni une liste « à ne pas
    faire ».
  - Il a listé 4 points déduits à faire confirmer : moment du tirage au sort, limite d'essais du
    placement, affichage des coordonnées, expiration des parties.
  - Hypothèses de Claude Code : la capacité horaire et toutes les estimations.
  - La relecture externe a pris position sur les 4 points et relevé que le socle ne laissait
    aucune marge pour les extensions. Elle a proposé un socle strict, avec la configuration
    (E0) et le placement manuel (E0bis) en extensions.
  - Les 4 arbitrages et le nouveau découpage ont été reportés dans `docs/REGLES.md` et
    `CONTEXTE-IA.md`.
  - Correction de traçabilité : Claude Code avait d'abord suggéré de présenter le calcul
    « 59 h + 8 h » comme une trouvaille du binôme. Ce calcul vient de la relecture externe.
- Décision et justification :
- Scénario ou commande de vérification :
- Résultat attendu, puis résultat observé :
- Erreur que ce contrôle pourrait détecter :
- Preuves reproductibles et limites :
  - Commits : `408aec9` (global.json), `57be076` (spécification des règles), `0daf9df`
    (contexte, backlog et périmètre).
  - Limites :

## 2026-09-15 — Phase moteur : représentation, placement, tours et masquage

- Outil / modèle si connu :
  - Claude Code (extension VS Code), modèle Claude Opus 5 : cadrage, options, implémentation
    par petits pas, tests, mutations, préparation des commits.
- Contexte : règles et périmètre fixés (`docs/REGLES.md`, `CONTEXTE-IA.md`), solution sans code
  métier. Il fallait le moteur du socle (S1 à S3, plus la vue révélée), testable sans serveur,
  avec `BattleShip.Models` sans dépendance.
- Prompt réellement utilisé :
  1. Cadrage de la représentation :
     ```text
     Phase moteur. Contrainte forte : BattleShip.Models ne référence aucun autre projet et n'a
     aucune dépendance à ASP.NET, JSON ou gRPC. Le moteur doit être testable sans serveur.

     Règles à implémenter : docs/REGLES.md, périmètre socle uniquement (grille 10x10, flotte
     par défaut, placement aléatoire une seule fois).

     Objectif de conception : un découpage clair et bien nommé, des frontières nettes entre
     domaine / règles / transport. Mais pas d'abstraction spéculative : pas d'interface
     introduite « au cas où », pas de couche qui ne sert qu'un seul appelant. Si une extension
     du backlog (E0 grille configurable, E2 niveaux d'IA) rend un choix meilleur qu'un autre,
     dis-le et explique pourquoi — c'est un argument valable, contrairement à « ça pourrait
     servir un jour ».

     Étape 1 — cadrage uniquement, pas de code :
     - propose 2 représentations possibles de la grille et de la flotte (par exemple grille de
       cases vs liste de navires + ensemble des tirs), avec ce que chacune rend facile ou
       pénible : détection de navire coulé, masquage de l'information, sérialisation, tests,
       et passage à une grille configurable plus tard ;
     - pour chaque option, dis où vit l'état d'une partie et pourquoi ce n'est pas dans Models
       si c'est ton avis ;
     - dis-moi laquelle tu recommandes et sous quelle condition tu changerais d'avis ;
     - propose les noms des types principaux, en une liste, sans code ;
     - liste les invariants du moteur que des tests devraient protéger.

     J'attends mon choix avant que tu écrives quoi que ce soit.
     ```
     Réponses données aux questions à choix de Claude Code :
     - Représentation : « B : navires + tirs »
     - État d'une partie : « API, namespace Engine »
     - Phases : « Oui : créer puis démarrer »
     - Mutabilité : « Mutable, état privé »

     Compléments envoyés au rejet du premier plan :
     ```text
     Deux ajouts avant de commencer :

     1. L'invariant 16 (cases de navire révélées = touchées + navires coulés) est le plus
        important du projet. Teste-le aussi en négatif : construis une partie où un navire
        non coulé a été touché une seule fois, et vérifie que RevealedBoard ne contient AUCUNE
        des autres cases de ce navire. Avant de l'exécuter, dis-moi quelle erreur
        d'implémentation ce test attraperait.

     2. Au pas 5, montre-moi que le test rougit si on casse le masquage : modifie
        volontairement RevealedBoard pour qu'il révèle tous les navires, lance le test,
        montre-moi l'échec, puis annule la modification. Je veux le avant/après pour ma
        revue IA n°1.

     Sur la numérotation ADR : docs/adr/0001-modele.md est le gabarit fourni par le cours, ne
     l'écrase pas. Le choix de représentation sera 0002-representation-grille-flotte.md, à
     préparer quand le moteur sera vert — pas maintenant.
     ```
  2. Définition d'un essai de placement : question à choix posée par Claude Code, « Dans « le
     placement aléatoire est limité à 1000 essais », qu'est-ce qu'un essai ? », avec trois
     options (une flotte complète, un tirage de position, 1000 tirages par navire). Réponse :
     « Une flotte complète ».
  3. Mesure de la limite de 1000 essais :
     ```text
     Du coup ton test des 1000 essais mesure un détail d'implémentation. Remplace le comptage
     de Random.Next par une mesure qui tient quelle que soit la façon de tirer : par exemple
     compter les essais via un point d'observation explicite, ou à défaut vérifier que TryPlace
     renvoie null et termine sous une seconde. Dis-moi ce que tu proposes avant de l'écrire, et
     quelle erreur la nouvelle version détecterait encore (notamment une limite à 999 ou 1001).
     ```
     Message de choix (les raisons données dans ce message ne sont pas recopiées ici) :
     ```text
     Option A.
     [...]
     Écris A avec un petit record plutôt qu'un out : PlacementResult(Board? Board, int
     AttemptsUsed) ou équivalent, le nom est à toi. Le test prend une configuration qui échoue
     sans aucun tirage (navire de 5 sur grille 3x3) et vérifie AttemptsUsed == 1000 exactement.
     Dis-moi quelle erreur il détecte encore et laquelle il ne détecte plus par rapport à B.
     ```
  4. Tour de l'ordinateur et fin de partie :
     ```text
     Feu vert pour le pas 4.

     Une contrainte de conception : le tour de l'ordinateur doit être inatteignable quand le
     tir du joueur est refusé — pas protégé par un `if (refusé) return;` placé avant, mais
     structurellement hors du chemin. Concrètement, si tu vois dans ton propre code un endroit
     où déplacer trois lignes ferait jouer l'IA après un refus, la structure est mauvaise.
     Montre-moi comment tu t'y prends.

     Et la mutation qui va avec : déplace volontairement le tour de l'IA pour qu'il s'exécute
     aussi sur un refus, montre-moi quel test rougit, puis annule. Si aucun test ne rougit,
     c'est qu'il en manque un.

     Question ouverte que je veux voir traitée : après un tir qui coule le dernier navire, le
     tireur garde la main (règle du rejeu) mais la partie est finie. Ces deux règles se
     télescopent. Dis-moi comment tu les concilies et quel test le prouve.
     ```
     Réponse à la question à choix « Au pas 4, Game enchaîne-t-il déjà la réponse de
     l'ordinateur, alors que la stratégie de ciblage n'arrive qu'en S4 ? » : « Oui, choix de
     case injecté ».
  5. Vue révélée :
     ```text
     Feu vert pour le pas 5. Trois exigences sur ce pas, c'est le plus important du projet :

     1. RevealedBoard doit être construit par liste blanche : on part de ce qui est révélé
        (tirs, navires coulés) et on n'énumère jamais les navires pour filtrer ensuite. Si ton
        implémentation contient une boucle sur Ships avec un if, dis-le-moi, c'est le mauvais
        sens.

     2. Le test négatif prévu (navire touché une fois, aucune autre case révélée) plus un cas
        qui me manque : un navire coulé adjacent à un navire non coulé. Je veux être sûr que
        révéler les cases du coulé ne révèle pas au passage une case du voisin.

     3. La mutation du masquage, exécutée : fais RevealedBoard révéler tous les navires, montre
        la sortie de dotnet test, annule. C'est la preuve avant/après de ma revue IA n°1, je
        veux les chiffres exacts.

     Et une question que je veux voir traitée : RevealedBoard est-il calculé à la demande
     depuis Board, ou stocké et mis à jour à chaque tir ? Dis-moi ce que tu as choisi et
     pourquoi — les deux ont des conséquences différentes sur la fuite d'information.
     ```
- Réponse et hypothèses résumées :
  - Échange 1 : Claude Code a comparé une grille de cases (A) et une liste de navires avec un
    ensemble de tirs (B), recommandé B, proposé de placer l'état d'une partie dans
    `BattleShip.API` plutôt que dans `Models`, listé les types et 16 invariants. Il a posé
    deux questions ouvertes (phase de préparation, mutabilité de `Game`).
  - Échange 2 : `REGLES.md` ne définissait pas ce qu'est un essai ; Claude Code a posé la
    question avant d'écrire le placement.
  - Échange 3 : Claude Code avait d'abord écrit un test qui comptait les appels à
    `Random.Next`. Le binôme a relevé que ce test mesurait un détail d'implémentation. Claude
    Code a proposé trois options (A : nombre d'essais renvoyé, B : point d'observation interne
    avec `InternalsVisibleTo`, C : chronomètre) et recommandé B ; le binôme a demandé A. Claude
    Code a signalé que A ne détecte plus une boucle infinie ni un compteur qui ne correspond
    pas au nombre réel d'essais. À la demande du binôme, un test d'échec après placement
    partiel (5×1, deux navires de 3) a été ajouté.
  - Échange 4 : Claude Code a proposé une boucle de l'ordinateur gardée par l'état de la partie
    (`Phase` et `CurrentTurn`) et non par le résultat du tir du joueur, un tour qui ne change
    que sur un tir accepté à l'eau, et `CurrentTurn` à `null` hors partie en cours pour
    concilier rejeu et victoire. Il a signalé que la garantie repose aussi sur l'invariant
    « aucune méthode publique ne rend la main avec un tour d'ordinateur en attente ». À la
    demande du binôme, le test du second démarrage a été renforcé et la boucle sans fin
    possible a été signalée par un commentaire.
  - Échange 5 : Claude Code a remplacé l'ensemble des tirs par un journal des réponses données
    au tireur, et construit la vue uniquement depuis ce journal, calculée à la demande et
    renvoyée figée. Il a signalé que ce changement modifie du code du pas 2. À la demande du
    binôme, la mutation de l'ordre d'enregistrement a été exécutée, ce qui a révélé un test
    manquant.
  - Correction de traçabilité : Claude Code avait proposé de reconstruire un état intermédiaire
    de `Board.cs` pour faire deux commits ; le binôme a refusé et demandé un commit unique.
- Décision et justification :
- Scénario ou commande de vérification :
  - `dotnet build BattleShip.slnx` puis `dotnet test BattleShip.slnx` à chaque pas.
  - Mutations exécutées puis annulées, chacune suivie de `dotnet test` :
    - placement : blocage des cases voisines supprimé ; `blocked.Add(cell)` supprimé ; limite
      à 1001 essais ; limite à 999 essais ;
    - partie : tir de l'ordinateur appelé hors de la garde d'état dans `PlayerFire` ; tour
      passé sur un tir refusé (`case ShotOutcome.Miss or null`) ; second démarrage qui refuse
      mais refait le tirage au sort ;
    - vue : `SunkShips` renvoie toute la flotte ; cases de la flotte ajoutées à `Misses` ;
    - journal : enregistrement avant tous les refus (A) ; enregistrement avant le refus hors
      grille seulement (B).
- Résultat attendu, puis résultat observé :
  - Attendu : chaque mutation fait échouer au moins un test ciblé ; le code restauré repasse
    entièrement.
  - Observé :
    - voisins non bloqués : 42 graines sur 50 en échec sur le test de contact par un côté ;
    - `blocked.Add(cell)` supprimé : aucun échec ; test ajouté, seul en échec ensuite ;
    - limite 1001 : `Expected: 1000, Actual: 1001` ; limite 999 : `Expected: 1000, Actual: 999` ;
    - tir de l'ordinateur hors garde d'état : 10 tests sur 17 en échec dans `GameTests` ;
    - tour passé sur un refus : 4 tests sur 17 en échec (`Expected: 4, Actual: 2`) ;
    - second démarrage qui refait le tirage : 10 graines sur 20 en échec ;
    - `SunkShips` = toute la flotte : 381 tests, 355 réussis, 26 en échec (tous les tests de
      `RevealedBoardTests`) ;
    - flotte ajoutée à `Misses` : 25 tests sur 26 en échec dans `RevealedBoardTests` ;
    - enregistrement avant tous les refus (A) : 381 tests, 351 réussis, 30 en échec ;
    - enregistrement avant le refus hors grille (B) : 381 réussis sur 381 ; après ajout de deux
      tests, 383 tests, 381 réussis, 2 en échec ;
    - code restauré en fin de phase : 383 réussis sur 383.
- Erreur que ce contrôle pourrait détecter :
  - une vue qui révèle un navire non coulé, entièrement ou par une case voisine ;
  - un tir refusé qui passe la main à l'ordinateur ou laisse une trace dans la grille ;
  - un placement qui autorise le contact par un côté ou le chevauchement de navires d'une case ;
  - une limite d'essais décalée de un.
- Preuves reproductibles et limites :
  - Commits : `a39a7d3`, `a12fabd`, `688be41`, `4db6565`, `5117a53`, `79cb391`, `d3dd499`.
  - Limites :
## 2026-09-15 — Phase API : stockage, endpoints et masquage sur le JSON brut (S5 à S7)

- Outil / modèle si connu :
  - Claude Code (extension VS Code), modèle Claude Opus 5 : cadrage, options, implémentation,
    tests, mutations, vérification de `api.http` avec curl, préparation des commits.
- Contexte : moteur et stratégie de l'ordinateur terminés (S1 à S4). Il fallait garder les
  parties en mémoire côté serveur, les exposer en Minimal API avec FluentValidation, et
  protéger l'invariant n°1 jusque dans les réponses HTTP, alors que `Board.Ships`,
  `Game.PlayerBoard` et `Game.ComputerBoard` sont publics.
- Prompt réellement utilisé :
  1. Cadrage S5 + S6 (début du message, sur le commit précédent, non recopié) :
     ```text
     S4 est terminé. On passe à S5 + S6 ensemble : le stockage des parties en mémoire et les
     endpoints Minimal API. Cadrage d'abord, pas de code.

     Points que je veux voir traités dans le cadrage :
     - les opérations exposées, avec route, entrée, réponse et codes HTTP d'erreur ;
     - les DTO, et comment tu garantis qu'aucun ne peut contenir un navire non coulé — je
       rappelle que Board.Ships, Game.PlayerBoard et Game.ComputerBoard sont publics ;
     - où vit l'état des parties, avec quelle durée de vie DI, et ce qui se passe si deux
       requêtes de tir arrivent en même temps sur la même partie ;
     - comment la stratégie de l'ordinateur est branchée depuis l'endpoint, sachant que le
       choix de case reçoit maintenant la vue ;
     - les enums sérialisés en chaînes, comme convenu.
     ```
     Réponses données aux questions à choix de Claude Code :
     - Masquage : « Convention + mapping par la vue » (autre option présentée : rendre
       `ComputerBoard` privé)
     - Concurrence : « Verrou + version attendue » (autres options : verrou par partie seul,
       verrou global)
     - Bornes : « Validateur minimal + moteur » (autre option : FluentValidation de 0 à 9)
     - Test JSON brut : « En S7, comme prévu » (autre option : dans S6)

     Validations suivantes, recopiées : « Tes noms devinés me vont : StaleVersion, la clé
     target, version à 0, 201 à la création. », « D'accord pour la version négative en 400,
     c'est bien une entrée mal formée et pas un conflit d'état. », « AlreadyStarted me va
     comme nom. »
  2. Ordre des contrôles et graine des tests d'endpoints :
     ```text
     Deux ajouts après les commits, avant le pas 4 :

     - Teste l'ordre des contrôles au niveau HTTP : un tir hors grille sur une partie pas encore
       démarrée doit renvoyer 409 NotStarted et pas 400. C'est un comportement contre-intuitif
       qu'un correcteur peut tester avec api.http, je veux qu'il soit couvert.

     - Le hasard réel dans les tests d'endpoints me gêne : tu dis toi-même que les vérifications
       dépendent de l'hypothèse que la partie n'est pas finie après un tir. Un test qui échoue
       une fois sur cinquante chez mon binôme est pire qu'un test absent. Propose-moi une façon
       de fixer la graine côté tests sans ajouter de code qui n'existe que pour ça.
     ```
     ```text
     Un point sur ta limite : « la graine rend la partie reproductible, pas pilotable ». Choisis
     la graine une fois, note dans un commentaire ce qu'elle produit (qui commence, et si un
     test en dépend), et ne la change plus. Si un futur test a besoin d'un scénario précis,
     c'est une autre graine documentée pour ce test-là, pas un changement global.

     D'accord pour un serveur de test par test. Mesure quand même le temps total de la suite
     avant/après : si on passe de 3 s à 30 s, on en reparle.
     ```
  3. Mutations de S6 :
     ```text
     Feu vert pour le pas 4.

     Sur la mutation « grille adverse traduite depuis Ships » : c'est la plus importante des
     quatre. Je m'attends à ce que La_grille_adverse_ne_revele_que_la_case_tiree la détecte,
     mais dis-moi combien de tests rougissent et lesquels. Si un seul test la voit, c'est une
     information à garder pour S7 — ça dirait que le masquage au niveau HTTP ne tient qu'à un
     fil.

     Pour la mutation du verrou : elle est probabiliste, tu l'as dit. Lance le test de
     concurrence plusieurs fois avec le verrou retiré et donne-moi le taux d'échec réel
     (par exemple 7 échecs sur 10 exécutions). Un chiffre vaut mieux qu'un « il peut échouer ».
     ```
  4. S7 (début du message, sur le commit précédent, non recopié) :
     ```text
     S7 : le test de masquage sur le JSON brut. Cadrage court, pas besoin d'un plan complet.
     Ce que je veux :
     - assertion sur la chaîne JSON de la réponse, pas sur l'objet désérialisé ;
     - un scénario partie neuve et un scénario partie en cours avec un navire touché non coulé ;
     - le test doit échouer sur la mutation 1a ET sur la 1b, vérifie-le en les rejouant ;
     - dis-moi ce qu'il détecte que le test typé ne détecte pas, et l'inverse.

     Une question de conception avant d'écrire : comment tu construis l'assertion sans
     connaître à l'avance les positions des navires ? Le test doit pouvoir les lire côté
     serveur pour savoir ce qui ne doit PAS apparaître.
     ```
     Réponse à la question à choix : « A. Partie construite à la main » (autre option : partie
     créée par HTTP, recherche limitée au sous-arbre `opponent`).
- Réponse et hypothèses résumées :
  - Cadrage : 4 routes (`POST /games`, `GET /games/{id}`, `POST /games/{id}/start`,
    `POST /games/{id}/shots`) ; DTO dans `BattleShip.Models/Dtos` ; `GameStore` enregistré en
    Singleton (`ConcurrentDictionary`) ; stratégie branchée par une fonction qui ne capture
    que `Random` ; `JsonStringEnumConverter` dans `Program.cs`. Claude Code a signalé que
    `Game` n'est pas thread-safe et qu'un `GET` pendant un tir peut lever « Collection was
    modified ».
  - Version attendue : compteur par partie dans `StoredGame`, hors du moteur, augmenté au
    démarrage réussi et à chaque tir accepté ; version dépassée → `409 StaleVersion`, vérifiée
    sous verrou avant l'appel au moteur.
  - Verrou : un `System.Threading.Lock` par partie ; `GameStore.Add` et
    `GameStore.TryExecute` calculent le DTO sous le même verrou que l'opération.
  - Validation : `FireRequestValidator` exige `Column`, `Row`, `ExpectedVersion` (champs
    nullables) et une version positive ou nulle ; `OutOfBounds` du moteur traduit en
    `400 ValidationProblem` sur la clé `target`. Ordre des réponses d'un tir : 400 champ
    manquant, 404, 409 `StaleVersion`, 409 `NotStarted`/`GameOver`, 400 hors grille,
    409 `AlreadyTargeted`, 200.
  - Au pas 1, seul `FireRequest` a été créé ; les autres DTO sont arrivés au pas 3 avec la
    traduction qui les remplit.
  - Graine des tests d'endpoints : Claude Code avait écrit que les vérifications
    dépendaient de l'hypothèse « partie non finie après un tir » ; le binôme a estimé le
    risque à une fois sur cinquante ; Claude Code a corrigé : cette fin de partie est
    quasi impossible (17 touches de l'ordinateur sans raté), le problème réel est la
    reproductibilité. Solution retenue : un serveur de test par test, `Random` remplacé par
    `new Random(20260915)` via `ConfigureTestServices`, sans code de production. Effet
    documenté en commentaire : l'ordinateur commence et rate en (8,4).
  - S7 : partie insérée dans le `GameStore` du serveur de test, flottes du joueur en colonnes
    0 à 4 et de l'ordinateur en colonnes 5 à 9, graine `PlayerStartsSeed = 1` ; positions
    relues côté serveur ; recherche dans toute la chaîne JSON ; garde-fou sur une chaîne
    légitime au même format. Claude Code a ajouté de lui-même une mutation 1c (fuite dans
    la section `player`) pour vérifier ce que le test typé ne voit pas.
  - Correction de traçabilité : sur la mutation du verrou, la première série de
    20 exécutions a été annoncée « 0 échec sur 20 ». C'était une erreur du script de
    comptage (recherche de la chaîne « Non réussi(s) », non trouvée à cause de l'encodage du
    « é »). Une exécution isolée a montré l'échec ; le recomptage sur le marqueur `[FAIL]`
    donne les chiffres ci-dessous.
- Décision et justification :
- Scénario ou commande de vérification :
  - `dotnet build BattleShip.slnx` puis `dotnet test BattleShip.slnx` à chaque pas.
  - API lancée (`dotnet run --project BattleShip.API --launch-profile http`) et séquence de
    `api.http` rejouée avec curl.
  - Graine : sonde temporaire exécutée deux fois, sorties comparées, puis supprimée.
  - Durée de la suite mesurée avant et après la graine.
  - Mutations exécutées puis annulées, suivies de `dotnet test` :
    - S6 : 1a `SunkShips` = toute la flotte adverse ; 1b `Hits` = toute la flotte adverse ;
      2 version jamais vérifiée dans `StoredGame.TryFire` ; 3 verrou retiré de
      `GameStore.TryExecute` (test de concurrence lancé 20 fois, puis 20 fois avec le
      verrou) ; 4 `JsonStringEnumConverter` retiré.
    - S7 : 1a et 1b rejouées ; 1c cases de la flotte adverse dans `player.misses`
      (`ToOwnBoardDto(game.PlayerBoard) with { Misses = [.. game.ComputerBoard.Ships.SelectMany(ship => ship.Cells)] }`
      dans `GameDtoMapper.ToStateDto`).
- Résultat attendu, puis résultat observé :
  - Attendu : chaque mutation fait échouer au moins un test ; le code restauré repasse
    entièrement.
  - Observé, `api.http` : 10 réponses conformes aux titres (201, 200, 409 `NotStarted`,
    200, 409 `AlreadyStarted`, 200, 409 `AlreadyTargeted`, 409 `StaleVersion`,
    400 `errors.target`, 400 `errors.Column`, 404).
  - Observé, graine : deux exécutions identiques (`navires joueur : 21,22,23,24,25 |
    02,03,04,05 | 17,27,37 | 57,58,59 | 81,91` ; `tirs d'ouverture de l'ordinateur : 1
    (84:Miss)`).
  - Observé, durée (476 tests) : avant 2,29 s et 2,32 s (xUnit) ; après 3,74 s, 2,52 s,
    2,42 s, 2,50 s.
  - Observé, mutations S6 (476 tests) :
    - 1a : 1 échec (`La_grille_adverse_ne_revele_que_la_case_tiree`) ;
    - 1b : 2 échecs (le même, et
      `Un_tir_depuis_une_version_depassee_renvoie_409_StaleVersion_sans_rien_changer`) ;
    - 2 : 2 échecs (`StoredGameTests.Un_tir_depuis_une_version_depassee_est_refuse_sans_toucher_la_partie`,
      `GameEndpointsTests.Un_tir_depuis_une_version_depassee_renvoie_409_StaleVersion_sans_rien_changer`) ;
    - 3 : premier comptage erroné « 0 échec sur 20 » ; recomptage : 20 échecs sur
      20 exécutions sans verrou (19 `System.InvalidOperationException : Operations that
      change non-concurrent collections must have exclusive access…`, 1
      `System.ArgumentException : An item with the same key has already been added. Key:
      Coordinate { Column = 6, Row = 0 }`) ; 0 échec sur 20 avec le verrou ;
    - 4 : 1 échec (`Les_enums_circulent_en_chaines`).
  - Observé, mutations S7 (478 tests) :
    - 1a : 3 échecs (les 2 tests de `MaskingJsonTests`, `La_grille_adverse_ne_revele_que_la_case_tiree`) ;
    - 1b : 4 échecs (les 2 tests de `MaskingJsonTests`, `La_grille_adverse_ne_revele_que_la_case_tiree`,
      `Un_tir_depuis_une_version_depassee_renvoie_409_StaleVersion_sans_rien_changer`) ;
    - 1c : 2 échecs, uniquement les 2 tests de `MaskingJsonTests` ; le test typé
      `La_grille_adverse_ne_revele_que_la_case_tiree` passe. Sortie de `dotnet test` :
      ```text
      [xUnit.net 00:00:00.88]     BattleShip.Tests.Api.MaskingJsonTests.Le_json_d_une_partie_en_cours_ne_revele_que_la_case_touchee_d_un_navire_non_coule [FAIL]
      [xUnit.net 00:00:00.88]       Assert.DoesNotContain() Failure: Sub-string found
      [xUnit.net 00:00:00.88]       String: ···"row":5}]],"misses":[{"column":6,"row":1},"···
      [xUnit.net 00:00:00.88]       Found:  "{"column":6,"row":1}"
      [xUnit.net 00:00:00.89]     BattleShip.Tests.Api.MaskingJsonTests.Le_json_d_une_partie_neuve_ne_contient_aucune_case_de_navire_adverse [FAIL]
      [xUnit.net 00:00:00.89]       Assert.DoesNotContain() Failure: Sub-string found
      [xUnit.net 00:00:00.89]       String: ···"row":5}]],"misses":[{"column":6,"row":1},"···
      [xUnit.net 00:00:00.89]       Found:  "{"column":6,"row":1}"
      Nombre total de tests : 478
           Non réussi(s) : 2
      ```
  - Observé, code restauré : 478 tests réussis sur 478.
- Erreur que ce contrôle pourrait détecter :
  - une grille adverse traduite depuis les navires au lieu de la vue révélée ;
  - une position de navire adverse non coulé présente n'importe où dans la réponse JSON ;
  - un tir joué depuis un état dépassé ;
  - des écritures simultanées sur une même partie ;
  - des enums envoyés en entiers ;
  - un contrôle des bornes placé avant celui de la phase.
- Preuves reproductibles et limites :
  - Commits : `f4b12f2` (validation des requêtes de tir), `06d2c81` (stockage, verrou et
    version), `a1243a1` (endpoints Minimal API), `788aa03` (graine des tests d'endpoints et
    ordre des contrôles), `42f68f2` (`api.http`), `6ad6134` (masquage sur le JSON brut).
  - Limites :

## 2026-09-16 — Historique et reprise des parties

- Outil / modèle si connu : GitHub Copilot dans VS Code ; exploration, implémentation et
  vérification ciblée.
- Contexte : l'accueil créait une partie mais ne listait pas les parties existantes. Le stockage
  utilisait déjà un `ConcurrentDictionary<Guid, Entry>` en mémoire.
- Prompt réellement utilisé : demande d'ajouter un historique de parties et la possibilité de
  reprendre les parties en cours, puis « fais au mieux » après présentation des options mémoire,
  fichier JSON et SQLite.
- Réponse et hypothèses résumées : l'option mémoire a été retenue pour rester cohérente avec
  l'architecture actuelle et ne pas ajouter de dépendance ; une partie `Setup` ou `InProgress`
  est reprenable par `/partie/{identifiant}`.
- Décision et justification :
- Scénario ou commande de vérification :
  - `dotnet build BattleShip.slnx`.
  - `dotnet test BattleShip.Tests/BattleShip.Tests.csproj --no-restore --filter "FullyQualifiedName~Une_partie_creee_apparait_dans_l_historique" -v minimal`.
- Résultat attendu, puis résultat observé :
  - Attendu : l'application compile et une partie créée apparaît dans `GET /games` en phase
    `Setup`.
  - Observé : build réussi ; test ciblé réussi `1/1`.
- Erreur que ce contrôle pourrait détecter : une partie créée absente de la liste ou un résumé
  exposant un mauvais état de reprise.
- Preuves reproductibles et limites :
  - Endpoint ajouté : `GET /games`.
  - DTO ajouté : `GameSummaryDto`.
  - Limite : les résumés et les états sont perdus au redémarrage de l'API.


## 2026-09-16 — Phase gRPC-Web : contrat, service, CORS et client Blazor (S8)

- Outil / modèle si connu :
  - Claude Code (extension VS Code), modèle Claude Opus 5 : cadrage, options, implémentation,
    tests, mutations, vérifications curl, rédaction de la procédure de démonstration.
- Contexte : socle HTTP terminé (S5 à S7). Le sujet impose un échange gRPC-Web fonctionnel depuis
  le navigateur, avec une réponse nominale et une erreur attendue démontrable, sans que
  `BattleShip.Models` prenne de dépendance gRPC.
- Prompt réellement utilisé :
  1. Cadrage (début du message, sur PROMPTS.md, non recopié) :
     ```text
     Ensuite, cadrage S8 (gRPC-Web). Pas de code. Ce que je veux dedans :
     - quelle opération passe en gRPC plutôt qu'en HTTP, avec 2 options et un argument qui
       tienne (pas « c'est plus rapide » sans mesure). J'en ferai un ADR ;
     - comment le .proto est partagé entre l'API (Server) et l'App (Client) sans que Models
       gagne une dépendance gRPC ;
     - où se branche FluentValidation sur le message entrant ;
     - quelle erreur attendue on démontre, et comment elle remonte jusqu'à l'écran ;
     - ce qu'il faut configurer côté API (UseGrpcWeb, EnableGrpcWeb, CORS avec les en-têtes
       gRPC exposés) et côté App (GrpcWebHandler) ;
     - comment le service gRPC partage le GameStore et la traduction en DTO avec les endpoints
       HTTP, sans dupliquer les règles ni contourner le verrou ;
     - ce qui risque de casser, pour qu'on ne cherche pas à l'aveugle.
     ```
  2. Feu vert du pas 1 et consignes :
     ```text
     Feu vert pour le pas 1.

     Deux précisions :

     1. Sur l'ADR 0003, l'argument à ne pas utiliser est la performance : on n'a mesuré aucun
        débit, et je ne défendrai pas un chiffre que je n'ai pas. L'argument qui tient est le
        contrat typé et les erreurs structurées par statut (NOT_FOUND, INVALID_ARGUMENT) plutôt
        qu'un ProblemDetails que le client doit interpréter. Prépare le squelette factuel avec
        les options A et B quand S8 sera vert.

     2. Sur le conflit de types côté tests : présente-moi les deux options avec l'erreur réelle
        si elle apparaît, comme tu le proposes. Ne tranche pas seul.

     3. Au fil de S8, consigne les faits pour l'entrée PROMPTS.md : les questions à choix que tu
        m'as posées avec toutes les options proposées et celle que j'ai retenue, mes prompts
        réellement envoyés, tes réponses résumées, les commandes et leurs sorties. Champs
        factuels seulement, comme d'habitude — Décision, Justification et Limites restent vides.
        Tu me donneras le bloc à la fin de la phase.
     ```
  3. Faits dans le dépôt et pas 2 :
     ```text
     ok pour le commit, mais le scratchpad est temporaire et tu y mets les faits de S8. Si la
     session se termine ou que le scratchpad est nettoyé, je perds la matière de PROMPTS.md.

     Écris s8-faits-prompts.md dans le dépôt, dans .notes/ (déjà gitignoré), plutôt que dans le
     scratchpad. Même chose pour les sorties de mutations que tu veux me garder.

     Ensuite, enchaîne le pas 2 : le service gRPC et sa validation. Sur le conflit de types
     côté tests, montre-moi l'erreur réelle et les deux options, ne tranche pas.
     ```
  4. Garde-fou du test de masquage gRPC (extrait) :
     ```text
     Une chose à ajouter avant le pas 3 : ton garde-fou ne mord que si la case légitime qu'il
     cherche est elle-même en colonne 0 ou ligne 0. Vérifie-le — retire WithFormatDefaultValues,
     lance le test, et dis-moi s'il rougit. S'il passe, le garde-fou ne protège pas contre cette
     erreur-là et il faut le renforcer (par exemple en plaçant volontairement une case en
     colonne 0 dans le scénario).

     Ensuite pas 3 : CORS et la configuration navigateur.
     ```
  5. Retrait de `RequireCors` et feu vert du pas 4 :
     ```text
     ok pour le commit, mais retire RequireCors(FrontPolicy) avant. Une ligne qu'aucun test ne
     protège et dont la suppression ne change rien donne une fausse impression de protection :
     si quelqu'un retire UseCors un jour, elle fera croire que le gRPC est couvert. Mets plutôt
     un commentaire d'une ligne au-dessus du MapGrpcService pour dire que la politique vient de
     UseCors et où elle est définie.

     Note l'inversion de l'ordre du pipeline par rapport à ton cadrage (UseGrpcWeb puis UseCors)
     dans les faits de S8 : c'est une correction en cours de route, elle a sa place dans
     PROMPTS.md.

     Feu vert pour le pas 4.
     ```
  6. Démonstration (extrait) :
     ```text
     Pour le pas 5 : lance l'API sur le profil https et vérifie de ton côté ce que tu peux
     (appel gRPC-Web et en-têtes CORS avec curl). Donne-moi ensuite la procédure exacte à suivre
     dans le navigateur : quelles commandes je lance, dans quel ordre, quelle URL j'ouvre, ce
     que je tape, et ce que je dois voir à l'écran ET dans l'onglet Réseau — pour le cas
     nominal comme pour les deux erreurs. Je la déroulerai moi-même.
     ```
  - Questions à choix posées par Claude Code, avec toutes les options :
    1. « Quelle opération passe en gRPC-Web ? » — A. GetGame (lecture d'état) ; B. Fire (tir).
       Retenue : **A**.
    2. « Où vit le fichier .proto ? » — `Protos/` à la racine ; dans `BattleShip.API/Protos`.
       Retenue : **`Protos/` à la racine**.
    3. « Comment les tests obtiennent-ils un client gRPC ? » — Option 1, génération côté tests
       (Grpc.Tools + `GrpcServices="Client"`) ; Option 2, `GrpcServices="Both"` dans l'API.
       Retenue : **Option 2**.
- Réponse et hypothèses résumées :
  - Cadrage : contrat hors de `Models`, FluentValidation appelé dans la méthode du service avec
    `RpcException(InvalidArgument)` en cas d'échec, erreur démontrée `NOT_FOUND` remontée par
    `grpc-status` et `grpc-message`, configuration API et App, service partageant `GameStore` et
    `GameDtoMapper`, et 9 risques listés dont les en-têtes non exposés et le contenu mixte.
  - Implémentation : `Protos/battleship.proto`, `GameGrpcService`, `GetGameRequestValidator`,
    `GameStateMessageMapper`, politique CORS `front`, client `GameService.GameServiceClient` et
    page `/reprise` côté App.
  - Corrections en cours de route :
    - ordre du pipeline : le cadrage annonçait `UseCors` puis `UseGrpcWeb` ; l'implémentation suit
      la documentation Microsoft, soit `UseGrpcWeb()` puis `UseCors(FrontPolicy)` ;
    - `RequireCors(FrontPolicy)` retiré du service gRPC à la demande du binôme après la mutation B ;
    - espace de noms du contrat renommé de `BattleShip.Grpc` en `BattleShip.Protocol` ;
    - garde-fou du test de masquage gRPC renforcé après l'expérience demandée par le binôme.
  - Claude Code n'a pas pu vérifier lui-même le comportement du navigateur : rendu de la page,
    exécution du client en WebAssembly et application réelle de la politique CORS.
- Décision et justification :
- Scénario ou commande de vérification :
  - `dotnet build BattleShip.slnx` et `dotnet test BattleShip.slnx` à chaque pas.
  - Essais du conflit de types côté tests (option 1 sans puis avec `Grpc.Tools`, option 2).
  - Expérience sur le garde-fou : `WithFormatDefaultValues(true)` retiré, avant et après
    renforcement du scénario.
  - Mutations CORS : `WithExposedHeaders` retiré ; `RequireCors(FrontPolicy)` retiré.
  - API lancée sur le profil https, appels `POST /battleship.GameService/GetGame` en curl avec un
    corps gRPC-Web binaire et l'en-tête `Origin: http://localhost:5274`, plus deux
    pré-vérifications `OPTIONS`.
  - Démonstration dans le navigateur déroulée par le binôme, procédure en 6 étapes.
- Résultat attendu, puis résultat observé :
  - Attendu : le service répond en gRPC-Web, les deux erreurs arrivent avec leur statut, et chaque
    mutation fait échouer au moins un test.
  - Génération : `GameServiceBase`, `GetGame`, `GamePhase { Unspecified = 0, Setup = 1,
    InProgress = 2, Finished = 3 }`, `HasCurrentTurn`.
  - Conflit de types : sans génération côté tests, `error CS0426: Le nom de type
    'GameServiceClient' n'existe pas dans le type 'GameService'` ; option 1 sans `Grpc.Tools`,
    élément `Protobuf` ignoré sans avertissement ; option 1 avec `Grpc.Tools`,
    **171 avertissements CS0436 et 2 erreurs CS1503** (`conversion impossible de
    'BattleShip.Grpc.GetGameRequest [BattleShip.Tests…]' en 'BattleShip.Grpc.GetGameRequest
    [BattleShip.API…]'`) ; option 2, **0 avertissement, 0 erreur, 4 tests gRPC-Web verts**.
  - Erreurs du pas 4 : sans `Grpc.Net.Client`, `error CS0234` sur `Grpc.Core` et `error CS0246`
    sur `ClientBaseConfiguration` ; puis `Pages/Reprise.razor(3,13): error CS0234: Le nom de type
    ou d'espace de noms 'Core' n'existe pas dans l'espace de noms 'BattleShip.Grpc'`, les `using`
    d'un composant Razor étant générés à l'intérieur de l'espace de noms de la page.
  - Garde-fou : sans `WithFormatDefaultValues` et avec une case touchée en (6,1), **aucun échec
    sur 486 tests** ; scénario renforcé (case en ligne 0), **1 échec** :
    `Assert.Contains() Failure: Sub-string not found — Not found: "{ "column": 6, "row": 0 }"`.
  - Mutations CORS : `WithExposedHeaders` retiré → 1 échec
    (`Une_reponse_au_front_expose_les_en_tetes_de_statut_grpc`) ; `RequireCors` retiré →
    **aucun échec sur 489 tests**, la politique globale s'appliquant déjà.
  - Appels curl en https : nominal `200 OK`, `Content-Type: application/grpc-web`, corps binaire ;
    partie inconnue `Grpc-Status: 5` et `Grpc-Message: Partie introuvable : elle n'existe plus sur
    le serveur.` ; identifiant invalide `Grpc-Status: 3` et `Grpc-Message: L'identifiant de la
    partie n'est pas valide.` ; les trois réponses portant
    `Access-Control-Expose-Headers: Grpc-Status,Grpc-Message,Grpc-Encoding,Grpc-Accept-Encoding`.
    Pré-vérification `OPTIONS` du front : `204`, `Allow-Origin: http://localhost:5274`,
    `Allow-Methods: POST`, `Allow-Headers: content-type,x-grpc-web`. Pré-vérification d'une origine
    inconnue : `204` sans aucun en-tête `Access-Control-*`.
  - Démonstration navigateur (déroulée par le binôme) : nominal `Phase Setup`, `Version 0`,
    `Tour personne`, `Mes navires 5`, `Cases adverses révélées 0`, avec `OPTIONS 204` puis
    `POST 200` ; partie introuvable après redémarrage de l'API, message « Cette partie n'existe
    plus sur le serveur », `grpc-status: 5`, HTTP 200 ; identifiant invalide, « Identifiant
    refusé », `grpc-status: 3`, HTTP 200.
  - Étape 6 (contrôle facultatif) : premier essai **invalide**, une instance de l'API lancée plus
    tôt tenait encore le port 7260 et verrouillait l'exécutable (`MSB3021`, `MSB3027`), donc la
    modification n'était pas compilée et le message affiché restait celui du cas normal.
    Expérience refaite par Claude Code avec le port libéré : sans `WithExposedHeaders`, la réponse
    ne porte plus `Access-Control-Expose-Headers` alors que `Grpc-Status: 5` est toujours présent
    dans les en-têtes. Observation complémentaire : dans une réponse nominale, le statut est dans
    une trame finale du corps (`grpc-status:0`), donc le cas nominal ne dépend pas de l'exposition
    des en-têtes.
  - Problème rencontré : certificat de développement non approuvé, la page affichant « Failed to
    fetch », message qui ressemble à un refus CORS sans en être un. Résolu par
    `dotnet dev-certs https --trust` puis redémarrage de l'API ; reporté dans le README.
  - État final : `dotnet build` 0 avertissement 0 erreur ; `dotnet test` 489 réussis sur 489.
- Erreur que ce contrôle pourrait détecter :
  - un statut gRPC illisible par le navigateur faute d'en-têtes exposés ;
  - une partie inconnue renvoyée comme un état vide au lieu de `NOT_FOUND` ;
  - une validation non exécutée avant l'accès au stockage ;
  - un message gRPC contenant une case de navire adverse non touchée ;
  - une traduction d'enum par transtypage, qui enverrait `Unspecified` pour une partie en
    préparation.
- Preuves reproductibles et limites :
  - Commits : `436ca32` (contrat), `99cf040` (service et tests), `0cb6aba` (CORS),
    `fa57417` (client Blazor et renommage en `BattleShip.Protocol`).
  - Sorties conservées dans `.notes/mutations/` : `s8-option1-build.txt`, `s8-option2-build.txt`,
    `s8-option2-test.txt`, `s8-pas2-test.txt`, `s8-pas3-test.txt`, `s8-cors-mutationA.txt`,
    `s8-cors-mutationB.txt`, `s8-gardefou-sans-defauts.txt`,
    `s8-gardefou-renforce-sans-defauts.txt`.
  - Limites :

## 2026-09-16 — Phase front : écran de jeu complet (S9)

- Outil / modèle si connu :
  - Claude Code (extension VS Code), modèle Claude Opus 5 : cadrage, options, implémentation,
    procédure de vérification manuelle.
- Contexte : socle serveur terminé (S5 à S8). Il fallait l'écran de jeu complet — création,
  deux grilles, tir, fin de partie, nouvelle partie — sans qu'aucune règle passe côté client.
- Prompt réellement utilisé :
  1. Cadrage (fin d'un message sur S8, début non recopié) :
     ```text
     Cadrage de S9 (le front complet) quand tu veux : création de partie, affichage des deux
     grilles, tir, fin de partie, nouvelle partie. Pas de code avant que je valide.
     ```
  2. Choix du binôme, donnés en texte libre et non par questions à choix :
     ```text
     Mes choix :
     1. URL /partie/{id}. Pas d'interop JS, identifiant visible et testable, URL partageable.
     2. gRPC-Web GetGame au chargement. Ça donne à S8 un usage réel dans le jeu plutôt qu'une
        page de démonstration à côté.
     3. Pas de bUnit. Le rendu se vérifie à l'œil dans le navigateur, et je préfère garder le
        temps restant pour les extensions. Procédure manuelle écrite comme au pas 5 de S8, et
        je l'assume dans les limites du README.
     4. Page + composant Grille réutilisé deux fois, avec un paramètre pour ma grille ou la
        grille adverse.

     Une contrainte sur le point 4 : le composant Grille ne doit rien savoir des règles. Il
     reçoit des listes de coordonnées et les affiche. S'il contient un if qui décide ce que le
     joueur a le droit de voir, c'est une règle qui a fuité côté client.

     Que devient la page /reprise ? Dis-moi si tu la supprimes ou si tu la gardes comme outil de
     démonstration — j'en ai besoin pour l'oral, mais pas forcément dans le menu.

     Enchaîne les pas 1 et 2 sans t'arrêter, montre-moi le résultat groupé.
     ```
     Options que Claude Code avait présentées pour ces quatre points : identifiant dans l'URL ou
     dans `localStorage` ; lecture de l'état par gRPC-Web ou par `GET /games/{id}` ; tests de rendu
     avec bUnit ou vérification manuelle ; page unique ou page plus composant `Grille`.
  3. Contrainte sur les tirs refusés :
     ```text
     Feu vert pour le pas 3. Une contrainte : quand un tir est refusé, l'état affiché ne doit pas
     bouger du tout. Pas de case qui change de couleur en optimiste avant la réponse, pas de
     compteur incrémenté. Le front n'anticipe rien, il affiche ce que le serveur renvoie.

     Enchaîne le pas 4 dans la foulée si le pas 3 est propre.
     ```
  4. Traitement des refus au démarrage, puis suppression de la page de démonstration :
     ```text
     Oui pour donner à StartAsync le même traitement que FireAsync au pas 5 : une exception sur
     un double clic, c'est un écran cassé pour un geste normal.
     ```
     ```text
     Supprime /reprise, ton argument tient : /partie/{id} démontre les deux erreurs gRPC et un
     écran en double est une dette pour rien. Fais-en un commit à part.
     ```
- Réponse et hypothèses résumées :
  - Structure : `Pages/Home.razor` (création), `Pages/Partie.razor` (jeu), `Components/Grid.razor`
    (affichage pur), `Services/GameApiClient`, `Services/GameStateConverter` (message gRPC → DTO),
    `TurnResult` (tour joué ou motif de refus).
  - Le composant `Grid` ne reçoit que des listes de cases et un rappel de clic : aucun paramètre
    « grille adverse », aucune condition sur ce que le joueur a le droit de voir.
  - Aucune anticipation : la page n'affiche rien avant la réponse du serveur, y compris sur un
    refus. `StaleVersion`, `AlreadyStarted` et `404` déclenchent une relecture de l'état.
  - Erreur rencontrée : `error CS0104: 'Coordinate' est une référence ambiguë entre
    'BattleShip.Protocol.Coordinate' et 'BattleShip.Models.Coordinate'`, réglée par un alias
    `Proto` dans la page.
  - Claude Code n'a pu vérifier ni le rendu ni les clics : aucun test ne couvre le front.
- Décision et justification :
- Scénario ou commande de vérification :
  - `dotnet build BattleShip.slnx` et `dotnet test BattleShip.slnx` à chaque pas.
  - Serveur de développement lancé : `GET /`, `GET /appsettings.json`, `GET /partie/{guid}`.
  - Script PowerShell jouant une partie entière par l'API, pour atteindre l'écran de fin.
- Résultat attendu, puis résultat observé :
  - `dotnet test` : 489 réussis sur 489 à chaque pas ; aucun test n'a été ajouté, le front n'étant
    pas couvert.
  - App servie : `index.html 200`, `{"ApiAddress": "https://localhost:7260"}`, `/partie/{guid}` en
    `200` (repli vers `index.html`).
  - Script de fin de partie exécuté : `Finished`, `gagnant Computer`, après 51 tirs du joueur.
- Erreur que ce contrôle pourrait détecter :
  - une configuration d'API absente du client ;
  - une route `/partie/{id}` non servie par le repli ;
  - une partie qui ne se termine jamais côté serveur.
- Preuves reproductibles et limites :
  - Commits : `87bbf67` (création, démarrage, deux grilles), `138c5fa` (tir, fin de partie,
    nouvelle partie), `b44494b` (refus au démarrage, procédure manuelle, README),
    `8415c17` (suppression de `/reprise`).
  - Procédure manuelle : `docs/VERIFICATION-MANUELLE.md`.
  - Limites :

## 2026-09-16 — Extension E0bis : placement manuel et tirage aléatoire

- Outil / modèle si connu :
  - Claude Code (extension VS Code), modèle Claude Opus 5 : cadrage, options, implémentation,
    tests, mutations.
- Contexte : socle terminé. `REGLES.md` prévoit que le joueur place lui-même sa flotte, avec la
  possibilité de la tirer au hasard tant que la partie n'a pas commencé.
- Prompt réellement utilisé :
  1. Cadrage :
     ```text
     Cadrage E0bis, pas de code. Ce que je veux dedans :
     - les opérations API à ajouter ou modifier, avec leurs codes d'erreur ;
     - comment le joueur place un navire : clic + rotation, ou autre chose — donne-moi 2 options
       d'interaction avec leurs limites, sans interop JavaScript ;
     - où vivent les règles de placement : elles existent déjà dans RandomFleetPlacer, dis-moi
       comment le placement manuel les réutilise sans les dupliquer ;
     - ce que devient le re-tirage aléatoire tant que la partie n'a pas commencé ;
     - quels tests existants sont impactés.
     ```
  2. Contrainte de structure au pas 1 :
     ```text
     Ton plan dit « ouvrir la grille du joueur pendant Setup ». Attention à ne pas fragiliser
     ce qui est protégé : Board reçoit aujourd'hui sa flotte à la construction et ne bouge
     plus. Si elle devient modifiable, dis-moi ce qui empêche un navire d'être ajouté après
     le démarrage, et quel test le prouve. Je ne veux pas d'un `if (phase == Setup)` posé au
     seul endroit qui appelle la méthode.

     Mutation à prévoir au pas 5, en plus des deux que tu listes : le placement manuel qui
     n'appelle pas PlacementRules et accepte n'importe quelle case. Je veux voir combien de
     tests rougissent — c'est la même question qu'en S6 sur le masquage, savoir si la règle
     n'est protégée que par un fil.
     ```
  3. Correction d'un motif de refus :
     ```text
     Sur NoShipToRemove après le démarrage : donne-lui NotInSetup. Le joueur qui clique « défaire »
     sur une partie commencée doit comprendre que la préparation est finie, pas croire qu'il n'a
     rien posé. Le message serait faux.
     ```
- Questions à choix posées par Claude Code, avec toutes les options :
  1. « Comment le joueur place-t-il un navire à l'écran ? » — A. sélection, rotation, clic sur
     l'origine, avec prévisualisation au survol (le client connaît la forme du navire) ;
     B. clic de début puis clic de fin, le serveur déduisant longueur et orientation (aucune
     géométrie côté client, mais plus de refus à expliquer). Retenue : **A**.
  2. « À quelle granularité le serveur valide-t-il le placement ? » — navire par navire (refus
     immédiat, aucun état de placement côté client) ; flotte complète en une fois (moins d'appels,
     refus tardif désignant le navire fautif). Retenue : **navire par navire**.
  3. « Les routes de placement portent-elles une version attendue, comme le tir ? » — oui, comme le
     tir ; non, pas en préparation. Retenue : **oui**.
- Réponse et hypothèses résumées :
  - `Board` reste immuable. Un type distinct, `FleetUnderConstruction`, porte la préparation ;
    chaque placement produit une nouvelle `Board`. Au démarrage, `Game` lâche cet objet (champ mis
    à `null`) : aucune opération de préparation n'a plus d'objet à modifier.
  - Les règles de placement sont extraites de `RandomFleetPlacer` vers `PlacementRules`, appelées
    à la fois par le placement aléatoire et par le placement manuel.
  - Routes : `POST /games/{id}/ships`, `POST /games/{id}/ships/undo` (remplacée plus tard par un
    retrait par case), `POST /games/{id}/fleet/random`, toutes avec version attendue ; le démarrage
    gagne le refus `FleetIncomplete`.
  - Écart au cadrage : `POST …/ships/undo` au lieu de `DELETE …/ships/last`, parce que
    `HttpClient.DeleteAsync` n'envoie pas de corps.
  - Ordre des contrôles constaté en rejouant `api.http` : la disponibilité de la longueur est
    vérifiée avant la géométrie. Le scénario « navire hors grille » du fichier a été corrigé en
    conséquence.
- Décision et justification :
- Scénario ou commande de vérification :
  - `dotnet build BattleShip.slnx` et `dotnet test BattleShip.slnx` à chaque pas.
  - `api.http` rejoué avec curl contre l'API réelle.
  - Mutations exécutées puis annulées : `PlacementRules` ignoré par le placement manuel ; version
    non vérifiée au placement ; flotte non lâchée au démarrage.
- Résultat attendu, puis résultat observé :
  - Cascade annoncée au cadrage puis observée : 10 tests en échec au premier passage du pas 1
    (9 dans `GameEndpointsTests`, 1 dans `GameGrpcServiceTests`), tous parce qu'une partie créée
    n'a plus de flotte joueur.
  - Compte de tests : 509 après le pas 1 (20 tests ajoutés), 529 après les pas 2 et 3 (20 de plus :
    11 pour les routes, 9 pour les validateurs, plus une assertion gRPC), inchangé aux pas 4 et 5.
  - Durée de la suite : environ 2,5 s avant l'extraction des règles, 3,8 s après, soit +1,3 s. La
    piste d'optimisation (ensemble de cases bloquées reconstitué dans le placeur) est notée en
    commentaire, sans être implémentée.
  - Mutations, sur 529 tests : `PlacementRules` ignoré par le placement manuel → **4 échecs**
    (`FleetPlacementTests.Un_navire_refuse_ne_change_rien_a_la_flotte`,
    `PlacementEndpointsTests.Un_navire_mal_place…` pour `Overlap` **et** `AdjacentShip`,
    `…Un_navire_qui_sort_de_la_grille_renvoie_400…`) ; version non vérifiée au placement →
    **1 échec** (`PlacementEndpointsTests.Poser_depuis_une_version_depassee_est_refuse_sans_rien_changer`) ;
    flotte non lâchée au démarrage → **24 échecs** (`FleetPlacementTests.Un_placement_apres_le_demarrage…`,
    `PlacementEndpointsTests.Apres_le_demarrage_la_flotte_ne_peut_plus_changer`,
    `StoredGameTests.Un_demarrage_reussi…`, `GameEndpointsTests.Demarrer_renvoie_200_puis_409_AlreadyStarted…`
    et les 20 graines de `GameTests.Demarrer_une_partie_deja_demarree…`).
  - Vérification curl : `api.http` rejoué contre l'API réelle, les 8 requêtes répondant comme leur
    titre l'annonce **sauf une** — « navire hors grille » demandait une longueur de 5 déjà posée, et
    le serveur répondait `409 LengthNotAvailable`. Scénario corrigé en longueur 4, puis revérifié :
    `400` sur la clé `origin`, message « Le navire sort de la grille. »
  - Écran de préparation (pas 4) et correction de l'aperçu (fin de pas 5) : aucun test ajouté, le
    front n'étant pas couvert ; le script de fin de partie, qui posait la flotte implicitement,
    appelle maintenant `/fleet/random` et a été réexécuté (`Finished`, `gagnant Computer`).
- Erreur que ce contrôle pourrait détecter :
  - un placement accepté sans passer par les règles ;
  - un placement joué depuis un état périmé ;
  - une flotte modifiable après le démarrage ;
  - un démarrage accepté avec une flotte incomplète.
- Preuves reproductibles et limites :
  - Commits : `a4c83ec` (préparation côté moteur), `18bceeb` (routes de placement),
    `e6a4956` (écran de préparation), `efdc119` (aperçu corrigé et procédure manuelle).
  - Limites :

## 2026-09-16 — Habillage visuel et ergonomie (en cours)

- Outil / modèle si connu :
  - Claude Code (extension VS Code), modèle Claude Opus 5 : cadrage, options, implémentation.
- Contexte : E0bis terminé. L'écran fonctionne mais reste sommaire ; le binôme veut un aperçu
  coloré, des navires dessinés, des gestes plus directs et un habillage lisible, sans qu'aucune
  règle passe côté client.
- Prompt réellement utilisé :
  1. Cadrage, sept priorités :
     ```text
     On passe à l'habillage visuel, avec quelques changements d'ergonomie. Cadrage d'abord,
     pas de code.

     Ce que je veux, par ordre de priorité :
     1. Aperçu au survol : contour vert si le placement est possible, rouge s'il ne l'est pas.
        Attention, c'est une règle : le client ne doit pas juger la validité. Propose-moi comment
        faire sans dupliquer les règles côté client — si la seule solution propre est un appel
        serveur de prévisualisation, dis-le et donne-moi le coût.
     2. Les navires ressemblent à des bateaux, pas à du texte. La liste des navires restants
        doit être visuelle : des formes de la bonne longueur qu'on sélectionne.
     3. Rotation avec la touche R et/ou la molette, plutôt qu'un bouton. Ça demande de l'interop
        JavaScript, ce qu'on avait écarté : dis-moi le coût exact et ce que ça ajoute comme
        limite. Garde le bouton en secours si l'interop échoue.
     4. Retirer un navire en cliquant dessus sur la grille, au lieu du bouton « Défaire ».
        Attention : aujourd'hui la route ne défait que le dernier posé. Dis-moi ce qu'il faut
        changer côté serveur.
     5. Cases plus grandes, survol visible, couleurs distinctes raté / touché / coulé, navires
        coulés marqués.
     6. Animations discrètes sur les tirs.
     7. Son, seulement si tout le reste est fini.

     Contraintes : aucune règle de jeu ne passe côté client, et l'invariant de masquage reste
     intact. Dis-moi ce qui touche au serveur et ce qui est purement front.
     ```
  2. Le port, idée du binôme :
     ```text
     Sur le port : oui, j'aime l'idée. Une zone type quai où les navires restants sont dessinés à
     leur vraie longueur, qu'on sélectionne au clic, et où un navire retiré de la grille revient à
     sa place. C'est ce que tu fais au pas 2, à la place de la liste de formes cliquables.

     Le glisser-déposer, on le garde pour après : dis-moi au pas 2 ce qu'il coûterait en plus
     (décalage du point de saisie, survol pendant le drag, retour au port, tactile), et je
     déciderai à ce moment-là. Ne l'anticipe pas dans la structure du composant.
     ```
  3. Décision sur le glisser-déposer :
     ```text
     D'accord avec ton avis sur le glisser-déposer : on ne le fait pas. Le clic couvre tout, et
     ajouter un chemin d'entrée non testé en fin de projet, c'est le mauvais moment. Note-le dans
     les arbitrages du README : envisagé, écarté, avec ta raison.

     Corrige le port pour qu'il dessine les navires dans l'orientation choisie — sinon le joueur
     ne voit pas ce qu'il va poser.
     ```
- Questions à choix posées par Claude Code, avec toutes les options :
  1. « Comment obtenir l'aperçu vert/rouge sans juger côté client ? » — A. prévisualisation par
     survol (`POST /ships/preview` à chaque case, des centaines d'appels, aperçu en retard s'il
     faut amortir) ; B. liste des origines valides renvoyée par le serveur (une dizaine d'appels
     pour toute la préparation) ; B2. origines valides incluses dans l'état (zéro appel
     supplémentaire, 15 à 20 Ko d'état et recalcul à chaque lecture). Retenue : **B**.
  2. « Retrait d'un navire : que devient la route "défaire" ? » — la remplacer par un retrait par
     case ; garder les deux. Retenue : **remplacer**.
  3. « Mes navires coulés dans le DTO de ma grille ? » — oui, le serveur les envoie ; non, on s'en
     passe. Retenue : **oui**.
- Réponse et hypothèses résumées :
  - Correction de traçabilité : Claude Code avait proposé « une rangée de formes cliquables ». La
    zone type quai, les navires à leur vraie longueur et le retour d'un navire retiré à sa place
    viennent du binôme.
  - Correction d'une erreur de cadrage de Claude Code : la rotation au clavier et à la molette ne
    demande **pas** d'interop JavaScript. Blazor gère `@onkeydown` et `@onwheel` nativement ; seul
    le focus initial passe par l'interop fournie par le framework.
  - `Board.Reveal()` calculait déjà mes navires coulés : aucun changement de moteur pour ce point,
    seulement le DTO et le `.proto`.
  - Coût du glisser-déposer chiffré par Claude Code : décalage du point de saisie, survol à refaire
    pendant le glisser, zone de retour au port, mode tactile séparé, état de glisser à maintenir,
    et trois à quatre scénarios de plus dans la procédure manuelle.
- Décision et justification :
- Scénario ou commande de vérification :
  - `dotnet build BattleShip.slnx` et `dotnet test BattleShip.slnx` à chaque pas.
  - Routes nouvelles rejouées avec curl contre l'API réelle.
- Résultat attendu, puis résultat observé :
  - `dotnet test` : 538 réussis sur 538, soit 9 tests ajoutés — origines valides côté moteur,
    quatre cas de la route `placements`, retrait par case, refus `NoShipHere`, et le mapper.
  - Aucune mutation exécutée à ce stade : elles sont prévues au pas 5, dont celle qui ferait juger
    la validité côté client.
  - Vérification curl : `{"length":4,"orientation":"Horizontal","origins":[{"column":6,"row":0},…]}`,
    liste vide pour une longueur déjà posée, `400` sur une requête sans `length`, `404` sur une
    partie inconnue, `200` puis `restantes : 4,3,3,2,5` au retrait par case, et
    `409 NoShipHere` sur une case vide.
  - Erreur commise puis corrigée par Claude Code : un test du mapper passait la grille du joueur en
    deuxième paramètre de `Game`, qui est celle de l'ordinateur ; le test a été corrigé, pas
    l'attendu.
- Erreur que ce contrôle pourrait détecter :
  - un aperçu qui jugerait la validité côté client ;
  - un retrait par case qui viserait le mauvais navire ;
  - des navires coulés déduits côté client au lieu d'être annoncés par le serveur.
- Preuves reproductibles et limites :
  - Commits : `6d7a024` (origines valides et retrait par case), `5772e6e` (aperçu coloré et port),
  `26ea545` (rotation clavier et molette, habillage des grilles).
  - Limites :

## 2026-09-16 — Phase 1 des extensions : retours visuels de tir

- Outil / modèle si connu : GitHub Copilot dans VS Code ; exploration ciblée, implémentation et
  compilation du projet Blazor.
- Contexte : la page de partie distinguait déjà les résultats du moteur, mais le retour visuel
  devait être enrichi sans déplacer de règle métier côté client.
- Prompt réellement utilisé : demande d'intégrer les animations de cases, les messages « Touché »,
  « Coulé » et « Raté », le nombre de navires restants et le verrouillage visuel pendant le tour
  de l'ordinateur.
- Réponse et hypothèses résumées : le moteur fournit déjà `Miss`, `Hit` et `Sunk`. La grille reçoit
  maintenant la cellule et le résultat à animer. Les navires restants sont calculés depuis l'état
  révélé ; aucune position adverse non découverte n'est ajoutée au DTO.
- Décision et justification :
- Scénario ou commande de vérification :
  - `dotnet build "BattleShip.App/BattleShip.App.csproj" -v minimal`.
- Résultat attendu, puis résultat observé :
  - Attendu : compilation du projet Blazor sans erreur.
  - Observé : `BattleShip.Models` et `BattleShip.App` compilés ; génération réussie en 3,2 s.
- Erreur que ce contrôle pourrait détecter : une erreur Razor ou C# dans le branchement des
  paramètres d'animation, du compteur ou du verrouillage visuel.
- Preuves reproductibles et limites :
  - Fichiers touchés : `BattleShip.App/Pages/Partie.razor`,
    `BattleShip.App/Components/Grid.razor`, `BattleShip.App/wwwroot/css/app.css`.
  - Aucun test automatisé de rendu Blazor n'a été exécuté.
  - Limites :

## 2026-09-16 — Phase 2 des extensions : niveaux d'IA

- Outil / modèle si connu : GitHub Copilot dans VS Code ; cadrage, implémentation par étapes,
  tests ciblés et compilations dans une sortie temporaire.
- Contexte : `HuntTargetStrategy` était la seule stratégie côté serveur. Le niveau devait être
  choisi avant la création, conservé dans `Game` et appliqué par l'API sans exposer la flotte
  adverse.
- Prompt réellement utilisé : demande de prendre la meilleure option parmi l'enum avec stratégies
  côté serveur et l'interface de stratégies, puis demande d'implémenter trois niveaux.
- Réponse et hypothèses résumées : l'option enum a été retenue. `Easy` choisit aléatoirement,
  `Normal` conserve la chasse-cible existante, et `Hard` calcule les placements compatibles avec
  les tirs révélés puis privilégie les cases les plus fréquentes.
- Décision et justification :
- Scénario ou commande de vérification :
  - `dotnet build "BattleShip.API/BattleShip.API.csproj" -v minimal -p:BaseOutputPath="obj\ai-validation\"`.
  - `dotnet build "BattleShip.App/BattleShip.App.csproj" -v minimal -p:BaseOutputPath="obj\ai-validation\"`.
  - `dotnet test "BattleShip.Tests/BattleShip.Tests.csproj" --no-restore --filter "FullyQualifiedName~HuntTargetStrategyTests" -v minimal -p:BaseOutputPath="obj\ai-tests-validation\"`.
  - Suite complète : `dotnet test "BattleShip.Tests/BattleShip.Tests.csproj" --no-restore -v quiet -p:BaseOutputPath="obj\ai-tests-validation\"`.
- Résultat attendu, puis résultat observé :
  - Les compilations API et App ont réussi ; les tests IA ciblés ont réussi `62/62`.
  - La suite complète ne présente plus que l'échec indépendant
    `FleetPlacementTests.Reprendre_un_navire_pose_avec_une_nouvelle_position_invalide_est_refuse_sans_rien_changer`.
    Elle attend `AdjacentShip` et observe `Overlap`.
- Erreur que ce contrôle pourrait détecter : une stratégie qui tire une case déjà jouée, qui
  consulte la flotte cachée ou qui n'est pas réellement branchée au niveau demandé.
- Preuves reproductibles et limites :
  - Fichiers touchés : `BattleShip.Models/AiDifficulty.cs`,
    `BattleShip.API/Engine/RandomTargetStrategy.cs`,
    `BattleShip.API/Engine/ProbabilityTargetStrategy.cs`, `BattleShip.API/Engine/Game.cs`,
    `BattleShip.API/Endpoints/GameEndpoints.cs`, `BattleShip.App/Services/GameApiClient.cs`,
    `BattleShip.App/Pages/Home.razor`, `BattleShip.App/Pages/Partie.razor`,
    `BattleShip.App/wwwroot/css/app.css`, et les tests associés.
  - Limites : l'échec de placement n'a pas été modifié car il est hors du périmètre de cette phase.

## 2026-09-16 — Phase 3 des extensions : statistiques de partie

- Outil / modèle si connu : GitHub Copilot dans VS Code ; exploration, implémentation et
  compilations ciblées.
- Contexte : le serveur exposait déjà le nombre de tirs et la durée dans les résumés, mais pas
  les tirs réussis, ratés, la précision ni l'historique dans l'écran de jeu.
- Prompt réellement utilisé : demande de poursuivre avec les statistiques de partie : tirs totaux,
  réussis, ratés, précision, durée, historique et résumé pendant/après la partie.
- Réponse et hypothèses résumées : les statistiques sont calculées côté serveur à partir des tirs
  acceptés du joueur, ajoutées à `GameStateDto` et transportées par gRPC-Web. L'historique ne porte
  que sur les cibles et résultats déjà connus du joueur.
- Décision et justification :
- Scénario ou commande de vérification :
  - `dotnet build "BattleShip.API/BattleShip.API.csproj" -v minimal -p:BaseOutputPath="obj\stats-validation\"`.
  - `dotnet build "BattleShip.App/BattleShip.App.csproj" -v minimal -p:BaseOutputPath="obj\stats-validation\"`.
- Résultat attendu, puis résultat observé : les deux compilations ont réussi ; l'API et le front
  ont généré leurs sorties sans erreur.
- Erreur que ce contrôle pourrait détecter : un contrat gRPC incomplet, un compteur modifié après
  un tir refusé ou une information non autorisée ajoutée à l'historique.
- Preuves reproductibles et limites : fichiers touchés : DTO statistiques, `StoredGame`, mapper
  HTTP/gRPC, proto, convertisseur Blazor, `Partie.razor` et `app.css`. Aucun test automatisé de
  statistiques n'a encore été ajouté.

## 2026-09-16 — Phases 4 et 5 des extensions : préparation et identité visuelle

- Outil / modèle si connu : GitHub Copilot dans VS Code ; implémentation progressive et
  compilations ciblées.
- Contexte : la préparation permettait déjà le placement manuel et aléatoire, mais pas la remise
  à zéro complète ; l'écran devait aussi rendre le début de partie, les tirs coulés et les sons
  plus perceptibles.
- Prompt réellement utilisé : demande de poursuivre avec réinitialisation, placement aléatoire
  relançable, indication de flotte complète, animations, sons optionnels et écran final enrichi.
- Réponse et hypothèses résumées : la réinitialisation est une commande serveur versionnée et
  reste impossible après le démarrage. Les sons utilisent Web Audio côté navigateur et peuvent
  être désactivés ; les règles restent côté serveur.
- Décision et justification :
- Scénario ou commande de vérification :
  - `dotnet build "BattleShip.API/BattleShip.API.csproj" -v minimal -p:BaseOutputPath="obj\fleet-validation\"`.
  - `dotnet build "BattleShip.App/BattleShip.App.csproj" -v minimal -p:BaseOutputPath="obj\fleet-validation\"`.
  - `dotnet build "BattleShip.App/BattleShip.App.csproj" -v minimal -p:BaseOutputPath="obj\identity-validation\"`.
- Résultat attendu, puis résultat observé : les compilations API et App ont réussi sans erreur.
- Erreur que ce contrôle pourrait détecter : une réinitialisation possible après le démarrage,
  une route non versionnée, une erreur Razor ou un appel JavaScript absent.
- Preuves reproductibles et limites : fichiers touchés : moteur et stockage de préparation,
  endpoint/client reset, `Partie.razor`, `index.html` et `app.css`. La procédure navigateur et
  les sons n'ont pas encore été vérifiés manuellement.

## 2026-09-16 — Ajustements ergonomiques et audio

- Outil / modèle si connu : GitHub Copilot dans VS Code ; corrections ciblées et compilations
  du front.
- Contexte : plusieurs retours manuels concernaient la rotation des bateaux, la hiérarchie des
  actions de préparation et l'intégration des contrôles audio.
- Prompt réellement utilisé : demandes de correction du pivot de rotation, de mise en avant du
  bouton `Commencer`, d'intégration des contrôles `Sons`/`Musique`, puis d'enrichissement de la
  musique et des effets.
- Réponse et hypothèses résumées : la rotation utilise désormais la case cliquée comme pivot
  côté client et serveur. `Commencer` est l'action principale et reste désactivé jusqu'à ce que
  la flotte soit complète. Les contrôles audio sont regroupés dans un panneau cohérent ; la
  musique est synthétisée côté navigateur pour éviter une ressource externe.
- Décision et justification :
- Scénario ou commande de vérification :
  - `dotnet build "BattleShip.App/BattleShip.App.csproj" -v minimal -p:BaseOutputPath="obj\pivot-validation\"`.
  - `dotnet build "BattleShip.App/BattleShip.App.csproj" -v minimal -p:BaseOutputPath="obj\audio-layout-validation\"`.
- Résultat attendu, puis résultat observé : les compilations du front ont réussi sans erreur.
- Erreur que ce contrôle pourrait détecter : une erreur Razor dans les contrôles, l'interop audio
  ou le nouveau chemin de rotation.
- Preuves reproductibles et limites : la vérification audio et le rendu exact de la rotation ont
  été compilés mais pas automatisés dans un navigateur. Les tests de rotation ont été adaptés au
  nouveau contrat de pivot ; un test indépendant de placement conserve un écart `Overlap`/`AdjacentShip`.
