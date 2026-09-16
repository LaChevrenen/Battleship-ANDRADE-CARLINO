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

**Preuves et limites**
La commande exécutée est :
`dotnet test BattleShip.Tests/BattleShip.Tests.csproj --no-restore --filter "FullyQualifiedName~Une_partie_creee_apparait_dans_l_historique" -v minimal`.
L'implémentation est en mémoire ; un redémarrage de l'API efface l'historique.
