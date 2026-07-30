# pmd-infinite-rogueessence

Fork de [`RogueCollab/RogueEssence`](https://github.com/RogueCollab/RogueEssence) portant les
**patchs moteur** du fangame **PMD Genèse**.

Ce dépôt n'est pas un projet autonome, et le badge « forked from » au-dessus est exact : le moteur
est l'œuvre de RogueCollab (MIT, © Audino 2020, contributions PsyCommando et AntyMew). Il est
gardé en fork **exprès**, pour rester rebasable sur l'upstream. Seuls les ajouts listés
ci-dessous sont propres au fangame — ils sont sur la branche `m2-biomes`, qui est la branche par
défaut de ce dépôt et celle que le jeu épingle en submodule. `master` est un miroir strict de
l'upstream, sans une ligne de modification.

## Ce que ce fork ajoute

**43 commits, 20 fichiers, ~1 600 lignes ajoutées** au-dessus de l'upstream.

| Ajout | Fichiers principaux |
|---|---|
| Méta-progression qui survit à la mort du personnage (fichier de sauvegarde dédié, écriture atomique hors du dossier de run) | `Data/MetaSave.cs` *(nouveau)*, `Data/GameProgress.cs` |
| Sélection de starter par quiz de type, et départ en solo | `Menu/Rogue/StarterTypeMenu.cs` *(nouveau)*, `Menu/Rogue/CharaChoiceMenu.cs`, `Menu/Rogue/RogueMenu.cs` |
| Scènes d'intro et de fin jouées dans le moteur | `Scene/EndScene.cs` *(nouveau)*, `Scene/TitleScene.cs`, `Scene/GameManager.cs` |
| Bindings Lua pour la monnaie méta et les améliorations achetables | `Lua/ScriptGame.cs` |
| Rendu « ombre seule » + invulnérabilité, pour un boss à mécanique dédiée | `Dungeon/Characters/Character.cs`, `Dungeon/DungeonScene.cs` |

## Écrit par des agents IA

Sur les 43 commits de ce fork, **41 sont écrits par des agents IA autonomes** et 2 par un humain.
Les agents tournaient sur deux machines — un Raspberry Pi qui orchestrait et poussait, un portable
x86 qui compilait et jouait — coordonnées par un bus de messages.

C'est aussi ce qui rend ce code intéressant comme banc d'essai : le critère de succès n'y est pas
« ça compile », puisqu'il y a un jeu à lancer. Le projet a d'ailleurs produit un cas d'école du
contraire — une quête validée en headless mais **injouable en vrai**, parce que son déclencheur
était câblé sur `OnEnterSegment`, donc avant `SetCurrentMap`. Seul un test avec affichage l'a vue.

## Le jeu

Le fangame qui consomme ce moteur vit dans un dépôt séparé, `pr0toboy/pmd-infinite`,
**actuellement privé**.

## Licence

MIT, comme l'upstream — voir [`LICENSE`](LICENSE). Le copyright du moteur reste à ses auteurs
d'origine.
