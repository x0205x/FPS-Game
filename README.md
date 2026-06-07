# FPS Game

Third-person FPS built with **Unity 6 LTS**, URP, Cinemachine 3, and the Input System.

- **GitHub:** [github.com/x0205x/FPS-Game](https://github.com/x0205x/FPS-Game)
- **Live demo (GitHub Pages):** [x0205x.github.io/FPS-Game](https://x0205x.github.io/FPS-Game/) *(after Pages is enabled)*

## Quick start (local)

1. Open the project in **Unity 6 LTS** (6000.x).
2. Open `Assets/Scenes/MainMenu.unity` and press **Play**.
3. For gameplay: **Start Prologue** loads `TestPlayground.unity`.

Useful editor menus:

| Menu | Purpose |
|------|---------|
| `Tools → Game → Build Main Menu Scene` | Sync menu assets + build order |
| `Tools → Game → Build Test Scene` | Rebuild test playground |
| `Tools → Game → Import Main Menu Font` | Regenerate Cinzel TMP font |
| `Tools → Game → Build WebGL Demo` | Export browser build to `docs/` |

## GitHub Pages demo (`docs/`)

The `docs/` folder hosts a static HTML demo page for GitHub Pages.

### Enable Pages (one-time)

1. Push this repo to GitHub.
2. Repo **Settings → Pages**
3. **Source:** Deploy from branch `main`
4. **Folder:** `/docs`
5. Save — site publishes at `https://<username>.github.io/FPS-Game/`

### Playable WebGL build

1. In Unity: **Tools → Game → Build WebGL Demo**
2. Build output lands in `docs/Build/` (+ Unity `index.html` may overwrite `docs/index.html`)
3. If the custom landing page was replaced, restore `docs/index.html` from git and keep the `Build/` folder
4. Commit and push `docs/`

## Project layout

See [Assets/README.md](Assets/README.md) for folder structure and phased roadmap.

## Scenes

| Scene | Purpose |
|-------|---------|
| `MainMenu.unity` | Cinematic menu (runtime UI bootstrap) |
| `TestPlayground.unity` | Gameplay / prologue test scene |

## License

Project assets include Unity template/sample content. Check third-party READMEs under `Assets/` before redistributing.
