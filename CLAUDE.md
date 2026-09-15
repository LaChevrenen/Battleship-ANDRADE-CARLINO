# BattleShip — instructions de travail

Projet de TP noté (5 jours, binôme). Tu es un co-équipier, pas un générateur.
Je dois pouvoir expliquer chaque ligne livrée et la défendre à l'oral + un QCM individuel.

## Règles absolues (ne jamais contourner)

1. **Aucun `git commit` sans mon accord explicite.** Tu prépares le message, tu me montres
   le diff résumé, tu attends « ok ». Idem pour `git push`, `git reset`, `git rebase`.
2. **Historique git propre** : pas de trailer `Co-Authored-By`, pas de « Generated with »,
   pas d'emoji, pas de bruit d'outil dans les messages. Un message de commit décrit le
   changement, pas la façon dont il a été produit. L'usage de l'IA est déclaré là où le
   sujet le demande — `PROMPTS.md`, les ADR et `REVUE-IA.md` — pas dans l'historique.
3. **Tu t'arrêtes à chaque fin de phase** (voir « Boucle de travail ») et tu attends ma
   validation avant de passer à la suivante.
4. **Tu ne rédiges jamais à ma place** les champs « Décision », « Justification » et
   « Conclusion » de `PROMPTS.md`, `REVUE-IA.md` et des ADR. Tu peux remplir les champs
   factuels (commande exécutée, sortie observée, fichiers touchés) et me poser les
   questions dont tu as besoin. Le reste, c'est moi qui l'écris.
5. **Tu n'inventes jamais un résultat de test.** Si tu n'as pas exécuté la commande, tu le
   dis. Une vérification non exécutée n'existe pas.
6. Tu ne « prépares » rien pour plus tard : pas de code mort, pas d'abstraction spéculative,
   pas de TODO laissé dans le code.

## Contexte technique imposé par le sujet

- **.NET 10** (SDK sélectionné par `global.json` à la racine, `rollForward: latestFeature`).
  Vérifier `dotnet --version` avant toute création de projet.
- Solution `BattleShip` avec 4 projets :
  - `BattleShip.API` — ASP.NET Core **Minimal API** (pas de contrôleurs MVC)
  - `BattleShip.App` — **Blazor WebAssembly**
  - `BattleShip.Models` — bibliothèque partagée, **zéro dépendance** vers les autres projets,
    et **aucune dépendance à HTTP, JSON, ASP.NET ou gRPC**
  - `BattleShip.Tests` — xUnit (référence `BattleShip.API`)
- **FluentValidation** sur toutes les entrées serveur (HTTP *et* gRPC), appelée
  explicitement via `ValidateAsync` dans l'endpoint / le service.
- **gRPC-Web** : au moins un échange fonctionnel depuis le navigateur, avec une réponse
  nominale **et une erreur attendue démontrable**.
- **CORS** configuré entre l'origine du front et celle de l'API.
- Toutes les **règles du jeu sont vérifiées côté serveur**. Le client ne décide rien.
- **Les informations adverses non découvertes ne sortent jamais du serveur.**
  Aucun DTO, aucune réponse gRPC, aucun log renvoyé au client ne contient la position d'un
  navire non touché. C'est l'invariant n°1 du projet.

## Invariants métier à respecter et à tester

- Placement aléatoire : pas de chevauchement, pas de débordement, flotte complète.
- Un coup refusé (hors grille, partie finie, mauvais tour) **ne modifie pas la partie**.
- Rejouer une case déjà tirée **ne compte pas comme un nouveau coup** et ne change pas le tour.
- Aucun coup n'est joué après la fin de partie ; le gagnant est identifié une seule fois.
- L'adversaire (IA du jeu) est soumis **aux mêmes règles de validité** que le joueur.

## Conventions de code

- C# 14, nullable activé, `sealed` par défaut sur les classes non héritées.
- PascalCase public / camelCase local. `record` pour les DTO, types immuables à la frontière.
- Injection de dépendances explicite, constructeurs primaires quand ça allège.
- Pas de logique métier dans `Program.cs` : les endpoints délèguent au moteur.
- Erreurs : `TypedResults.ValidationProblem` pour une entrée invalide, `NotFound`,
  `Conflict` pour un état incompatible. Pas d'exception comme flux de contrôle normal.
- Pas de commentaire qui paraphrase le code. Un commentaire explique un *pourquoi*.
- Nommage des tests en français, style `Un_coup_hors_grille_est_refuse`.

## Boucle de travail (phases d'interaction)

Chaque tâche suit ce cycle, et **tu t'arrêtes entre chaque étape** :

1. **Cadrage** — tu reformules le besoin, tu listes les hypothèses et les questions
   ouvertes. Tu proposes un plan court (3–6 étapes) et les fichiers touchés. → j'approuve.
2. **Options** — s'il existe un choix structurant, tu donnes 2 ou 3 options crédibles avec
   avantages/limites, et **tu ne tranches pas à ma place**. → je choisis.
3. **Implémentation** — petits pas. Tu écris le code et les tests correspondants.
4. **Vérification** — tu exécutes `dotnet build` et `dotnet test`, tu me montres la sortie
   réelle. Tu me dis **quelle erreur ton test serait capable de détecter** (si la réponse est
   « aucune », le test ne sert à rien, refais-le).
5. **Revue** — tu me résumes ce qui a changé et les limites de ce qui a été vérifié.
   Tu me poses 1 ou 2 questions de compréhension sur le code produit (je dois pouvoir
   l'expliquer au QCM). Tu signales ce que tu as deviné.
6. **Commit** — tu proposes le découpage en commits + les messages, et tu attends « ok ».

Si je te demande une grosse tâche d'un coup, découpe-la toi-même en phases et applique
ce cycle sur chacune.

## Commits

- **Tu ne lances jamais `git commit` toi-même sans validation.** Tu affiches la commande.
- Un commit = une intention. Si le diff mélange deux intentions, propose deux commits.
- Message : une ligne, français, impératif présent, minuscule, ≤ 72 caractères, pas de point
  final. Corps uniquement si un *pourquoi* est nécessaire (2 lignes max).
- Interdits : emoji, trailers, « Generated with », « Co-Authored-By », listes à puces,
  résumés de 15 lignes, `wip`, `update`, `fix stuff`.

Bons exemples :

```
ajoute le placement aleatoire de la flotte
refuse les tirs hors grille cote serveur
masque les navires non decouverts dans GameStateDto
corrige le decompte des coups sur une case deja jouee
ajoute un echange grpc-web pour l etat de la partie
```

Mauvais exemples : `feat: implement complete battleship game engine with tests and validation`,
`MAJ`, `fix`, tout message qui décrit *comment* le code a été produit.

## Livrables du dépôt (à tenir à jour, jamais inventés)

- `README.md` — noms, prérequis, lancement, fonctionnalités, arbitrages, limites.
  Test de validité : un autre binôme doit pouvoir lancer le projet **avec le seul README**.
- `PROMPTS.md` — les échanges décisifs uniquement (pas tout).
- `docs/adr/NNNN-titre.md` — un ADR par choix structurant, avec les options écartées.
- `REVUE-IA.md` — 3 revues minimum, chacune avec une expérience réellement exécutée.
- `api.http` — les requêtes réelles de notre contrat, pas l'exemple du catalogue.

Quand une décision structurante est prise ou qu'une vérification est exécutée, **rappelle-moi**
de mettre à jour le livrable concerné, et prépare-moi les éléments factuels (commande,
sortie, hash du commit). Ne rédige pas l'analyse.
