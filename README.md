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

- `http://localhost:5274/` : créer une partie.
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
- **Placement manuel de sa flotte** : choix du navire, bascule horizontal/vertical, aperçu au
  survol, « Défaire », et « Placement aléatoire » autant de fois que voulu tant que la partie n'a
  pas commencé. La flotte de l'ordinateur est placée par le serveur.
- Toutes les règles sont appliquées côté serveur : placement, tours, rejeu tant qu'on touche,
  refus explicites, victoire.
- Ordinateur « chasse-cible » : il tire au hasard, puis vise les cases voisines jusqu'à couler.
- Les positions des navires adverses non coulés ne sortent jamais du serveur.
- Reprise d'une partie par son URL `/partie/{identifiant}`, l'état étant relu en gRPC-Web.
- Requêtes HTTP d'exemple dans `api.http`.

## Arbitrages

## Limites connues

- Le front n'est couvert par aucun test automatisé. Sa vérification se fait à la main, avec la
  procédure `docs/VERIFICATION-MANUELLE.md`.
- Les parties vivent en mémoire et n'expirent pas : un redémarrage du serveur les perd toutes, et
  la page affiche alors « Cette partie n'existe plus sur le serveur ».
- Le client dessine la forme d'un navire pour l'aperçu au survol, donc il connaît sa géométrie.
  Il ne juge jamais la validité d'un placement : chevauchement, contact et débordement sont
  refusés par le serveur, qui reste seul juge.
