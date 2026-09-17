# Règles du jeu — référence commune

Les règles marquées **[E0]** (configuration de la partie) et **[E0bis]** (placement manuel)
sont des extensions choisies. Le socle se joue sur une grille 10×10, avec la flotte par défaut
et un placement aléatoire.

## Configuration d'une partie

- **Grille** : 10 colonnes × 10 lignes par défaut. **[E0]** Le joueur peut choisir une grille
  rectangulaire de **5 à 15 colonnes** et de **5 à 15 lignes**.
- **Flotte par défaut** : 1 navire de 5 cases, 1 de 4, 2 de 3 et 1 de 2 (17 cases).
- **[E0] Flotte personnalisée** : tailles de 1 à 5 cases, **de 0 à 3 navires par taille**, au
  moins 1 navire en tout.
- **Symétrie** : les deux camps (joueur et ordinateur) ont exactement la même grille et la
  même flotte.
- **[E0]** Le serveur **refuse une configuration qu'il ne parvient pas à placer** dans la limite
  d'essais du placement aléatoire (voir ci-dessous), même si un placement existe peut-être en
  théorie.
- **[E0] Attaques spéciales** : réservées à la partie personnalisée, où elles sont activées par
  défaut et désactivables. Une partie classique ne les a **jamais** activées. Désactivées, la
  jauge des deux camps reste à zéro pour toute la partie et une attaque spéciale est refusée
  quelle que soit la situation.

## Coordonnées

- À l'écran, les colonnes sont désignées par des lettres (A à O) et les lignes par des nombres
  (1 à 15).
- Entre le navigateur et le serveur, une case est toujours désignée par deux **indices
  numériques** (colonne, ligne). Une notation du type « B7 » ne circule jamais sur le réseau.

## Placement des navires

- Un navire occupe une ligne droite de cases, horizontale ou verticale.
- Interdit : sortir de la grille, chevaucher un autre navire, **toucher un autre navire par un
  côté**. Le contact en diagonale est autorisé.
- **Joueur** : **[E0bis]** par défaut, il place ses navires lui-même. Il peut aussi demander un
  placement aléatoire, et le redemander autant de fois qu'il veut **tant que la partie n'a pas
  commencé**. Chaque placement, manuel ou aléatoire, est vérifié par le serveur.
- **Ordinateur** : sa flotte est placée au hasard par le serveur, selon les mêmes règles.
- Le placement aléatoire est limité à **1000 essais**. Au-delà, il échoue au lieu de chercher
  indéfiniment.
- La partie commence quand la flotte du joueur est complète et valide.

## Déroulement

- **Qui commence** : tiré au sort par le serveur **au démarrage de la partie**, une fois le
  placement terminé, et non à sa création.
- **Tir** : le tireur désigne une case de la grille adverse. Trois résultats possibles :
  *à l'eau*, *touché*, *coulé*.
- **Rejouer** : le tireur **garde la main tant qu'il touche** (un tir « coulé » compte comme
  un tir qui touche). Après un tir *à l'eau*, la main passe à l'adversaire.
- **Coulé** : quand toutes les cases d'un navire sont touchées, le tireur est prévenu. Il voit
  quelles cases occupait ce navire, donc sa taille. Rien d'autre n'est révélé.
- **Victoire** : le premier camp dont tous les navires adverses sont coulés gagne. La partie
  s'arrête **immédiatement**, même au milieu d'une série de tirs. Le gagnant est désigné une
  seule fois.

## Attaque spéciale

- **Jauge** : chaque camp a sa propre jauge, qui avance d'une unité à chacun de ses tirs
  **acceptés** (simple ou spécial), qu'il touche ou rate. Elle se remplit tous les
  **5 tirs acceptés** de ce camp et **plafonne à une charge** : une fois pleine, elle
  n'avance plus tant que la charge n'a pas été utilisée.
- **Déclenchement** : une fois chargée, le tireur peut demander une attaque spéciale au lieu d'un
  tir simple, sur la même route que le tir — le serveur ne l'impose pas, c'est un champ optionnel
  de la requête. Sans charge, la demande est **refusée** (`SpecialAttackNotCharged`) et ne change
  rien. Le client de référence l'envoie **automatiquement** dès que la jauge du joueur est pleine :
  aucun geste supplémentaire, le tir suivant devient l'attaque spéciale de lui-même.
- **Effet** : elle résout **jusqu'à 5 cases en un seul coup** — la case visée et ses 4 voisines
  par les côtés (une croix). Les voisines hors grille ou déjà tirées sont **simplement ignorées**,
  sans provoquer de refus : seule la case visée est soumise aux motifs de refus habituels d'un
  tir (hors grille, déjà tirée, mauvais tour, partie non commencée ou terminée). Si la case visée
  est refusée, l'attaque entière l'est aussi et rien n'est joué.
- **Rejeu** : le tireur garde la main si **au moins une** des cases résolues est *touché* ou
  *coulé*. La jauge revient à zéro après usage, que l'attaque touche ou non.
- **Statistiques et historique** : comptée comme **un seul tir**, avec un résultat agrégé (le
  meilleur des résultats individuels : coulé > touché > à l'eau) et le premier navire coulé
  rencontré parmi les cases résolues, le cas échéant.
- **Symétrie** : l'ordinateur suit exactement la même règle. Il utilise son attaque spéciale
  automatiquement dès qu'elle est chargée, sur la case que sa stratégie de tir aurait choisie de
  toute façon — aucune heuristique de décision séparée.

## Tirs refusés

Un tir est **refusé**, avec un message explicite, dans ces cas : case hors grille, **case déjà
tirée**, ce n'est pas le tour du tireur, partie pas encore commencée, partie terminée, ou attaque
spéciale demandée **sans charge** disponible.

Un tir refusé **ne change rien** : ni la grille, ni le tour, ni le nombre de coups, ni une
éventuelle série en cours. L'ordinateur ne joue pas en réponse à un tir refusé.

## L'ordinateur

- Il est soumis **aux mêmes règles** que le joueur : ses tirs passent par les mêmes
  vérifications, et il ne tire jamais hors grille ni deux fois sur la même case.
- Trois niveaux, choisis par le joueur pendant la préparation et modifiables tant que la partie
  n'a pas commencé :
  - **Facile** : il tire au hasard parmi les cases jamais tirées.
  - **Normal** : stratégie dite « chasse-cible ». Il tire au hasard jusqu'à toucher un navire,
    puis vise les cases voisines jusqu'à l'annonce « coulé ».
  - **Difficile** : il évalue, pour chaque case libre, le nombre de placements de navires encore
    compatibles avec ce qu'il sait, et vise la case la plus probable.
- Quel que soit le niveau, il ne dispose que des informations qu'aurait un joueur humain : les
  résultats de ses propres tirs, et rien d'autre.

## Information cachée

Le joueur ne reçoit **jamais**, tant que la partie se joue, la position d'un navire adverse
qu'il n'a pas coulé. Il connaît seulement ses propres navires, les résultats de ses tirs, les
navires adverses coulés et les tirs de l'ordinateur sur sa grille.

**Exception, à la toute fin d'une partie perdue seulement** : les navires adverses jamais coulés
sont alors révélés, en plus de ceux déjà coulés — il n'y a plus de partie à protéger derrière
leur position. Cette exception ne s'applique ni en cours de partie, ni en cas de victoire (où
tout est de toute façon déjà coulé).

## Reprise

Si le joueur recharge la page, il retrouve sa partie dans l'état où il l'a laissée, qu'elle
soit en préparation, en cours ou terminée. Si le serveur ne connaît plus la partie (par exemple
après un redémarrage), le joueur en est informé et peut en créer une nouvelle.

Les parties **n'expirent pas** : une partie abandonnée reste sur le serveur jusqu'à son arrêt.
