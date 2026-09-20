# ADR 0003 : opération exposée en gRPC-Web

## Statut et date
Accepté — 2026-09-16.

## Contexte
Le sujet impose au moins un échange gRPC-Web fonctionnel depuis le navigateur, avec une réponse
nominale et une erreur attendue démontrable. L'API expose déjà en HTTP les quatre opérations du
socle : créer une partie, lire son état, la démarrer, tirer (S6, commits `f4b12f2` à `6ad6134`).

Contraintes qui pèsent sur le choix :
- `BattleShip.Models` ne peut prendre aucune dépendance gRPC ou Protobuf, et la solution est
  limitée à 4 projets : le `.proto` vit donc dans `Protos/` à la racine, compilé en serveur côté
  API et en client côté App.
- Toute règle du jeu est vérifiée côté serveur, et l'invariant n°1 (aucun navire adverse non coulé
  ne sort du serveur) doit tenir sur ce second transport comme sur HTTP.
- Les parties sont gardées en mémoire, avec un verrou par partie et une version attendue pour les
  tirs (ADR 0002 et S5).
- Aucune mesure de débit ou de latence n'a été faite : un argument de performance ne peut pas être
  utilisé ici.

## Options envisagées

### Option A — lire l'état d'une partie (`GetGame`)
- L'erreur attendue correspond à une situation réelle de `docs/REGLES.md` : après un redémarrage du
  serveur, la partie n'existe plus et le joueur doit en être informé.
- Contrat typé et erreurs portées par un statut (`NOT_FOUND`, `INVALID_ARGUMENT`) plutôt qu'un
  `ProblemDetails` que le client doit interpréter.
- L'opération ne modifie rien : ni version attendue, ni traduction des refus de tir à écrire une
  seconde fois. Toutes les modifications de partie restent sur le chemin HTTP, déjà testé et muté.
- Limite : l'entrée à valider se réduit à un identifiant.
- Limite : le client dispose de deux façons de lire l'état, `GET /games/{id}` et `GetGame`, et il
  faut dire laquelle l'écran utilise.

### Option B — tirer (`Fire`)
- FluentValidation porterait sur une entrée métier complète (colonne, ligne, version attendue), et
  plusieurs erreurs deviendraient démontrables : `NOT_FOUND`, `INVALID_ARGUMENT`, `ABORTED` pour une
  version dépassée, `FAILED_PRECONDITION` pour une partie terminée.
- Limite : deux chemins modifieraient une partie. Il faudrait extraire la logique de `FireLocked`
  hors de l'endpoint pour la partager, sous peine d'écrire deux fois la vérification de version et
  la traduction des refus.
- Limite : chaque motif de refus du moteur devrait recevoir un équivalent en statut gRPC.
- Limite : les tests et les mutations de S6 seraient à refaire sur ce second chemin.

## Décision
On a décider de mettre ne place le gRPC pour status du jeu car c'étais l'option la plus interresante à faire


## Conséquences
Constatées pendant l'implémentation (commits en Références) :

- **Contrat** : `Protos/battleship.proto`, espace de noms C# `BattleShip.Protocol`. Ce nom a
  remplacé `BattleShip.Grpc` : dans un composant Razor, les `using` sont générés à l'intérieur de
  l'espace de noms de la page, et `Grpc.Core` s'y résolvait en `BattleShip.Grpc.Core`
  (`error CS0234`).
- **Valeurs absentes** : `current_turn` et `winner` sont `optional`, et chaque enum commence par une
  valeur `…_UNSPECIFIED = 0`. La traduction des enums est explicite et non un transtypage, les
  numéros ne coïncidant pas avec ceux de `BattleShip.Models`.
- **Masquage** : le message est construit à partir de `GameStateDto`, jamais de `Game`. Un test
  vérifie l'absence de case adverse non touchée dans le message converti en JSON, avec les valeurs
  par défaut, et un garde-fou sur une case en ligne 0.
- **Partage avec HTTP** : le service lit l'état par `GameStore.TryExecute` puis
  `GameDtoMapper.ToStateDto`, donc sous le même verrou et avec la même traduction que
  `GET /games/{id}`.
- **Erreurs** : elles sont renvoyées en levant `RpcException`, ce qui est la façon documentée en
  gRPC ASP.NET Core, et une exception à la convention « pas d'exception comme flux de contrôle ».
- **CORS** : politique `front` avec `WithExposedHeaders("Grpc-Status", "Grpc-Message",
  "Grpc-Encoding", "Grpc-Accept-Encoding")`, et pipeline `UseGrpcWeb()` puis `UseCors(FrontPolicy)`.
  Sans ces en-têtes exposés, le navigateur cache le statut au code de l'App.
- **`RequireCors` retiré** du service gRPC après mutation : `UseCors(FrontPolicy)` s'applique déjà à
  toutes les requêtes, et aucune ligne de test ne protégeait cet appel.
- **Client de test** : l'API génère aussi le client (`GrpcServices="Both"`). Le générer une seconde
  fois dans le projet de tests dupliquait chaque type de `BattleShip.Protocol` (171 avertissements
  CS0436 et 2 erreurs CS1503).
- **Prérequis d'exécution** : sans certificat de développement approuvé, l'appel depuis la page
  échoue avec « Failed to fetch », message qui ressemble à un refus CORS. Reporté dans le README.

## Vérification et réexamen
Pour verifier que cela marchais nous avons utiliser, le système de résumer de parties

## Références
- Règles : `docs/REGLES.md`. Contrat : `Protos/battleship.proto`.
- Commits de la phase :
  - `436ca32` ajoute le contrat grpc de lecture de l etat d une partie
  - `99cf040` ajoute le service grpc-web de lecture de l etat d une partie
  - `0cb6aba` autorise le front en cors et expose les en tetes de statut grpc
  - `fa57417` branche l app sur le service grpc-web de reprise
- Vérifications : `dotnet test BattleShip.slnx` → 489 réussis sur 489 ; appels curl en https
  (nominal, `Grpc-Status: 5`, `Grpc-Status: 3`, pré-vérifications) ; démonstration dans le
  navigateur déroulée par le binôme (nominal, partie introuvable après redémarrage, identifiant
  invalide).
