# Revues de propositions IA

## Revue 1 — le masquage des navires non coulés

**Proposition examinée**
RevealedBoard et la méthode Reveal() de Board : la vue de la grille adverse envoyée au joueur (commit d3dd499).

**Hypothèse**
Le joueur ne doit jamais recevoir la position d'un navire adverse qu'il n'a pas coulé. Notre garantie repose sur la structure de la vue : elle est construite à partir du journal des réponses déjà données au joueur, et ne lit jamais la flotte. Si c'est vrai, une erreur dans la vue ne peut pas faire sortir un navire jamais touché.

**Expérience**
Deux mutations successives. La première fait renvoyer toute la flotte par SunkShips. La seconde fait fuiter les cases des navires par Misses.

**Résultat attendu avant exécution**
Les tests de la vue devaient rougir dans les deux cas, en particulier le test négatif (un navire touché une seule fois ne révèle aucune de ses autres cases).

**Résultat observé**
Avant mutation : 381 tests, tous verts.
Mutation SunkShips : 355 réussis, 26 échecs — exactement les 26 tests de RevealedBoardTests, aucun autre. Le test négatif affiche le navire entier qui fuit : Assert.Empty() Failure … [Coordinate { Column = 2, Row = 2 }, { Column = 3, Row = 2 }, { Column = 4, Row = 2 }].
Mutation Misses : 25 échecs sur 26. Les deux assertions DoesNotContain du test négatif et du test du voisin se déclenchent.
Après annulation : 381 tests verts.

**Décision et justification**
Vue acceptée telle quelle. La seconde mutation a été ajoutée parce qu'avec la première, le test négatif échouait dès sa première assertion : les deux assertions suivantes ne s'exécutaient jamais. Sans cette seconde mutation, on ne saurait pas si elles servent à quelque chose — elles auraient pu être fausses ou inutiles sans qu'on le voie.

**Preuves et limites**
Commit d3dd499. La garantie ne vaut qu'au niveau du moteur : Board.Ships, Game.PlayerBoard et Game.ComputerBoard sont publics, rien n'empêche techniquement un endpoint de les sérialiser directement. L'invariant ne sera protégé de bout en bout qu'avec le test sur le JSON brut prévu en S7. Aucune fuite par canal auxiliaire n'a été examinée : temps de réponse, taille du payload, ordre des champs.


## Revue 2 — une ligne de code que personne ne testait

**Proposition examinée**
La ligne blocked.Add(cell) dans RandomFleetPlacer, qui marque comme interdite la case occupée par un navire (commit 688be41).

**Hypothèse**
Chaque ligne de l'algorithme de placement protège une règle, et un test la couvre.

**Expérience**
Suppression de la ligne, puis relance de la suite de tests.

**Résultat attendu avant exécution**
Au moins un test de placement devait rougir : sans cette ligne, deux navires devraient pouvoir se superposer.

**Résultat observé**
Aucun test ne rougit. En cherchant pourquoi : sur un navire de deux cases ou plus, chaque case est déjà bloquée en tant que voisine orthogonale d'une autre case du même navire. La ligne n'est donc indispensable que pour un navire d'une seule case — qui n'existe pas dans la flotte par défaut.

**Décision et justification**
Garder la ligne et ajouter le test qui la couvre : Deux_navires_d_une_case_ne_peuvent_pas_occuper_la_meme_case. Avec la ligne supprimée, il est le seul à échouer. La ligne est correcte et ne coûte rien. La supprimer créerait un bug silencieux dès qu'on ajoutera la flotte configurable (E0), qui autorise les navires d'une case. Le test la protège en attendant.

**Preuves et limites**
Commit 688be41. Le test ne mord que sur des navires d'une case, donc sur une configuration qui n'est pas atteignable dans le socle. Il ne prouve rien sur la flotte par défaut.


## Revue 3 — un tir refusé qui laissait une trace

**Proposition examinée**
L'ordre des vérifications dans Board.ReceiveShot : le tir n'est enregistré dans le journal qu'après avoir écarté tous les motifs de refus (commit d3dd499).

**Hypothèse**
Un tir refusé, c'est un tir qui n'a pas eu lieu : il ne doit rien changer dans la partie, ni le tour, ni les cases enregistrées. C'est différent d'un tir valide mais manqué, qui lui laisse une marque visible. Nous pensions que nos tests couvraient ça, puisqu'ils testaient les refus.

**Expérience**
Introduction volontaire d'un bug dans Board.ReceiveShot : déplacer l'enregistrement du tir avant le contrôle « hors grille », pour qu'une case invalide entre quand même dans le journal. Le but était de vérifier que nos tests savaient détecter ce genre d'erreur.

**Résultat attendu avant exécution**
Au moins un des tests de refus devait rougir.

**Résultat observé**
Les 381 tests passent. Aucun ne voit le problème.

**Décision et justification**
Ajout de deux tests : un tir hors grille répété doit renvoyer OutOfBounds les deux fois, et un tir refusé ne doit pas apparaître dans la vue. Avec le bug, ils échouent tous les deux. Bug annulé, 383 tests verts.
Cette mutation a été faite parce qu'elle avait été signalée comme manquante et que nous avons voulu la traiter plutôt que de la laisser de côté. Le trou n'était pas théorique : sans ces tests, retirer sur une case hors grille aurait renvoyé « déjà tirée » au lieu de « hors grille », et la vue aurait affiché un raté en dehors du plateau.

**Preuves et limites**
Commit d3dd499. Ces deux tests ne couvrent que les refus qui passent par Board. Les refus gérés par Game (partie non démarrée, mauvais tour, partie finie) arrivent avant tout appel à Board et n'ont pas été testés de cette façon. Un tir refusé parce que la case est déjà tirée ne peut pas laisser de trace, puisque la case est déjà dans le journal.
Ce que ça nous a appris : des tests verts ne prouvent que ce qu'on a cherché à casser.


## Revue 4 — liste et reprise des parties

**Proposition examinée**
Ajouter un résumé public par partie et l'afficher sur l'accueil, sans exposer les grilles.

**Hypothèse**
Un résumé limité à l'identifiant, la date, la phase et le gagnant permet de reprendre une partie
en cours sans divulguer la position des navires adverses.

**Expérience**
Création d'une partie par HTTP, lecture de `GET /games`, désérialisation en `GameSummaryDto`,
puis vérification de la phase `Setup` et de l'absence de gagnant.

**Résultat attendu avant exécution**
La partie créée devait être présente dans la liste avec la phase `Setup` et un gagnant nul.

**Résultat observé**
Le build a réussi et le test
`Une_partie_creee_apparait_dans_l_historique_et_reste_reprenable` a réussi : `1` test, `0` échec.

**Décision et justification**
Nous avons fait ce choix dans le but d'avoir un moyen test. Mais aussi de proposer une manière plus simple de venir reprendre une partie qui avait été mis en pause.

**Preuves et limites**
La commande exécutée est :
`dotnet test BattleShip.Tests/BattleShip.Tests.csproj --no-restore --filter "FullyQualifiedName~Une_partie_creee_apparait_dans_l_historique" -v minimal`.
L'implémentation est en mémoire ; un redémarrage de l'API efface l'historique.


## Revue 5 — retours visuels après un tir

**Proposition examinée**
Ajouter dans l'écran de partie une animation courte de la case jouée, les messages distincts
`Raté`, `Touché` et `Coulé`, le nombre de navires restants et un verrouillage visuel de la grille
adverse pendant le tour de l'ordinateur.

**Hypothèse**
Ces améliorations peuvent rester côté Blazor : le moteur et le serveur continuent de fournir les
résultats et l'état autorisés, sans décision de règle dans le composant de grille.

**Expérience**
Compilation ciblée du projet front après modification de `Partie.razor`, `Grid.razor` et `app.css`.

**Résultat attendu avant exécution**
Le projet Blazor devait compiler sans erreur Razor ou C#.

**Résultat observé**
Commande exécutée : `dotnet build "BattleShip.App/BattleShip.App.csproj" -v minimal`.
`BattleShip.Models` et `BattleShip.App` ont été compilés ; génération réussie en 3,2 s.

**Décision et justification**
Même si des retour visuel/audio avait été mis en place pour informer le joueur sur la partie actuel, cela restait pas assez. L'ajout du nombre de bateau restant et devenu une nécessité dans le cadre de grille customiser à le nombre différent à 5 devenait difficile à gerer pour l'utilisateur.

**Preuves et limites**
La compilation ne vérifie pas le rendu visuel dans un navigateur et aucun test automatisé de rendu
Blazor n'a été exécuté.


## Revue 6 — niveaux de difficulté de l'ordinateur

**Proposition examinée**
Ajouter trois niveaux d'IA appliqués côté serveur : aléatoire, chasse-cible et probabiliste.

**Hypothèse**
Le mode difficile peut choisir ses cibles à partir de `RevealedBoard` uniquement, en évaluant les
placements encore compatibles avec les tirs connus, sans lire les navires cachés.

**Expérience**
Compilation séparée de l'API et de l'App, puis exécution des tests ciblés des stratégies et de la
sélection de navire.

**Résultat attendu avant exécution**
Les stratégies devaient compiler ; le mode facile ne devait jamais rejouer une case et le mode
difficile devait privilégier une case compatible avec une touche connue.

**Résultat observé**
Les compilations API et App ont réussi dans `obj\\ai-validation`. Les tests ciblés ont réussi
`62/62`. Une première version du test probabiliste a échoué, puis a révélé que les placements
contenant une touche connue étaient exclus ; la correction a fait repasser les tests.

**Décision et justification**
Nous avons choisi trois niveaux de difficulté car nous avions 3 idée d'algorythme. Ont a choisit que c'est algorythme n'ait pas accès aux vrais cases pour ne aas créer de la difficulté artificielle, pour se battre à armes égales.

**Preuves et limites**
La suite complète laisse un échec indépendant dans `FleetPlacementTests` : `AdjacentShip` attendu,
`Overlap` observé. Aucun changement n'a été fait sur ce test ou sur le placement pendant cette phase.


## Revue 7 — statistiques et historique des tirs

**Proposition examinée**
Ajouter les statistiques de partie et un historique des tirs acceptés dans l'état serveur, puis
les afficher pendant la partie et sur l'écran final.

**Hypothèse**
Les statistiques calculées depuis le journal serveur restent cohérentes après rechargement et ne
révèlent aucune case adverse non découverte.

**Expérience**
Compilation séparée de l'API et de l'App après ajout du contrat DTO, du message gRPC-Web et de
l'affichage Blazor.

**Résultat attendu avant exécution**
Le contrat et l'affichage devaient compiler sans erreur.

**Résultat observé**
API compilée avec succès ; App Blazor compilée avec succès en 3,4 s dans la sortie temporaire
`obj\\stats-validation`.

**Décision et justification**
On a décider de faire les calculs à partir du journal on non d'une autres façon dans le but que ces statiques soit accessible après la fin d'une partie.

**Preuves et limites**
La vérification exécutée est une compilation ; aucun test automatisé n'a encore exercé le calcul
de précision ou la persistance de l'historique après rechargement.


## Revue 8 — préparation de flotte et identité visuelle

**Proposition examinée**
Ajouter la réinitialisation complète de la flotte, l'indication de flotte prête, une animation de
lancement, un effet de tir renforcé et des sons désactivables.

**Hypothèse**
La réinitialisation doit être exécutée par le serveur avec la même version attendue que les autres
opérations de préparation ; les animations et les sons peuvent rester côté interface.

**Expérience**
Compilation ciblée de l'API après ajout de la route de reset, puis compilation de l'App après les
changements d'interface et d'interopérabilité Web Audio.

**Résultat attendu avant exécution**
Les deux projets devaient compiler sans erreur.

**Résultat observé**
La compilation API a réussi en 2,1 s dans `obj\\fleet-validation`. La compilation App a réussi
en 3,7 s dans `obj\\identity-validation`.

**Décision et justification**


**Preuves et limites**
Les clics de reset, le rendu exact des animations et l'écoute des sons n'ont pas été vérifiés dans
un navigateur pendant cette passe.


## Revue 9 — corrections ergonomiques et audio

**Proposition examinée**
Corriger le pivot des bateaux, renforcer la hiérarchie des actions de préparation et intégrer une
ambiance audio cohérente dans le lobby et la partie.

**Hypothèse**
Le pivot doit être décidé par la case cliquée et validé par le serveur ; l'audio peut rester dans
le front avec une musique synthétisée, des effets distincts et des contrôles regroupés.

**Expérience**
Compilations ciblées du front après les corrections de rotation, de boutons et d'audio.

**Résultat attendu avant exécution**
Le front devait compiler sans erreur Razor, JavaScript embarqué ou interopérabilité.

**Résultat observé**
Les compilations ciblées `pivot-validation` et `audio-layout-validation` ont réussi.

**Décision et justification**

**Preuves et limites**
Le rendu navigateur et le niveau sonore n'ont pas été mesurés automatiquement. Les tests de
rotation ont été adaptés au pivot sélectionné ; un échec indépendant du placement reste signalé.
