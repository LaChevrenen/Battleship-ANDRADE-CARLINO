# Battleship-ANDRADE-CARLINO

Bataille navale jouable dans le navigateur : API ASP.NET Core (Minimal API et gRPC-Web) et client
Blazor WebAssembly.

## Binôme

## Prérequis

- **.NET SDK 10.** La version est fixée par `global.json` (`10.0.100`, `rollForward: latestFeature`).
  Vérifier avec `dotnet --version`.
- **Certificat de développement HTTPS approuvé** :

  ```powershell
  dotnet dev-certs https --trust
  ```

  Accepter la fenêtre Windows, puis redémarrer l'API. Sans cette étape, l'appel gRPC-Web depuis la
  page échoue avec « Failed to fetch » dans la console du navigateur. Le message ressemble à un
  refus CORS, mais vient du certificat.

- **Port 7260 libre avant de relancer l'API.** Une instance laissée ouverte garde le port et
  verrouille l'exécutable : la recompilation échoue (`MSB3021`, `MSB3027`) et le navigateur continue
  de parler à l'ancien binaire, sans aucun message qui l'indique. Vérifier avec :

  ```powershell
  netstat -ano | Select-String ":7260.*LISTENING"
  ```

## Lancer le projet

Deux terminaux, depuis la racine du dépôt :

```powershell
# API : http://localhost:5062 et https://localhost:7260
dotnet run --project BattleShip.API --launch-profile https

# Client Blazor : http://localhost:5274
dotnet run --project BattleShip.App --launch-profile http
```

L'App appelle l'API à l'adresse indiquée dans `BattleShip.App/wwwroot/appsettings.json`
(`https://localhost:7260`). La politique CORS de l'API autorise `http://localhost:5274` et
`https://localhost:7172`.

Pages disponibles :

- `http://localhost:5274/` : créer une partie, reprendre les parties en cours et consulter
  l'historique des parties terminées.
- `http://localhost:5274/partie/{identifiant}` : jouer, et reprendre la partie après un
  rechargement. L'état y est relu par gRPC-Web.

Requêtes HTTP prêtes à l'emploi : `api.http`.

## Vérifier l'installation

```powershell
dotnet build BattleShip.slnx
dotnet test BattleShip.slnx
```

## Fonctionnalités

- Partie contre l'ordinateur sur une grille 10 × 10, flotte 5, 4, 3, 3 et 2 cases.
- **Placement manuel de sa flotte** : port des navires restants dessinés à leur vraie longueur,
  aperçu au survol coloré en vert ou en rouge selon ce que le serveur autorise, molette pour
  orienter le navire qu'on a en main, et « Placement aléatoire » autant de fois que voulu tant
  que la partie n'a pas commencé. La flotte de l'ordinateur est placée par le serveur.
- **Reprendre un navire déjà posé** : cliquer dessus le retire de la grille et le remet en main,
  dans l'état d'un navire à poser. Il se réoriente à la molette et se repose où l'on veut. Un
  navire resté sur la grille ne pivote jamais sous la molette.
- **Trois niveaux pour l'ordinateur** — Facile, Normal, Difficile — choisis **pendant la
  préparation**, et modifiables tant que la partie n'a pas commencé.
- Toutes les règles sont appliquées côté serveur : placement, tours, rejeu tant qu'on touche,
  refus explicites, victoire.
- Les positions des navires adverses non coulés ne sortent jamais du serveur.
- Reprise d'une partie par son URL `/partie/{identifiant}`, l'état étant relu en gRPC-Web.
- Historique en mémoire des parties créées, avec distinction entre parties en cours et parties
  terminées depuis l'accueil ; le bouton « Reprendre » ouvre leur URL, et une partie peut être
  supprimée.
- **Statistiques de fin de partie** : nombre de tirs, tirs au but, tirs à l'eau, pourcentage de
  réussite, durée, et historique des tirs.
- **Sons et musique**, réglables depuis la **barre latérale**, donc depuis n'importe quelle page.
  Tout est synthétisé dans le navigateur, sans ressource externe : effets de tir, et deux
  ambiances musicales — une nappe calme à l'accueil et pendant la préparation, un thème rythmé
  pendant la partie.
- **Panneau de règles** ouvert par le bouton `?`, en bas à droite de l'écran de jeu.
- Requêtes HTTP d'exemple dans `api.http`.

## Arbitrages

- **Glisser-déposer pour poser les navires : envisagé, écarté.** Le clic couvre déjà le besoin :
  sélection du navire dans le port, aperçu au survol, clic pour poser, clic sur un navire posé pour
  le retirer. Le glisser aurait ajouté, en fin de projet, un chemin d'entrée qu'aucun test ne
  couvre : décalage entre la case saisie et l'origine posée, survol à refaire pendant le glisser,
  zone de retour au port, et un mode tactile séparé puisque le glisser HTML5 ne fonctionne pas au
  doigt.

## Limites connues

- Le front n'est couvert par aucun test automatisé. Sa vérification se fait à la main, avec la
  procédure `docs/VERIFICATION-MANUELLE.md`.
- Les parties vivent en mémoire et n'expirent pas, historique compris : un redémarrage du serveur
  les perd toutes, et la page affiche alors « Cette partie n'existe plus sur le serveur ».
- **L'orientation d'un navire ne se change qu'à la molette.** Sans souris à molette — sur un écran
  tactile, par exemple — il n'existe aucun moyen de poser un navire verticalement. Le bouton
  « Orientation » a été retiré au profit du geste, et aucun équivalent clavier ne l'a remplacé.
- **`POST /games/{id}/ships/rotate` et `POST /games/{id}/ships/move` ne sont plus appelées par
  l'écran.** Elles restent exposées, testées et présentes dans `api.http` : reprendre un navire en
  main couvre les deux besoins côté interface. Voir `docs/adr/0004`.
- `BattleShip.Tests` référence `BattleShip.App` avec `Aliases="app"`, et les tests d'écran
  commencent par `extern alias app;`. Sans cet alias, les types générés depuis `battleship.proto`
  existeraient deux fois — l'API les génère en `Both`, l'App en `Client` — et tout le projet de
  tests ne compilerait plus (`CS0433`).
- Le client dessine la forme d'un navire pour l'aperçu au survol, donc il connaît sa géométrie.
  Il ne juge jamais la validité d'un placement : chevauchement, contact et débordement sont
  refusés par le serveur, qui reste seul juge.
