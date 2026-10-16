# TestPlayableAds

Test technique de playable ad Unity : un taquin 4x4 exporté avec le plugin Luna.

## Contenu

- `Assets/Scenes/SampleScene.unity` : contient l'objet `PuzzleGame`.
- `Assets/Scripts/PuzzleGame.cs` : le jeu (uGUI construit par code, aucun asset externe). Tactile et souris, mise en page portrait et paysage.
- `Assets/Scripts/PlayableCta.cs` : appels Luna `Playable.InstallFullGame()` (bouton CTA et écran de fin) et `LifeCycle.GameEnded()` (victoire), actifs sous `UNITY_LUNA`.

## Build

Le plugin Luna (Unity Playworks 7.2.0) est référencé dans `Packages/manifest.json` via `file:../../7.2.0/scripts` : le dossier SDK doit être à côté du projet (non versionné). Builder depuis la fenêtre Luna. L'URL du store et le comportement MRAID par réseau se règlent dans le Luna Playground.
