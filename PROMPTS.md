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
