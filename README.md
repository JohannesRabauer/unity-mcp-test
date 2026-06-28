# Neon City — Top-Down Action Sandbox

A small GTA-style, top-down action game built in **Unity 6** and entirely generated/driven through the [Unity MCP](https://github.com/CoplayDev/unity-mcp) workflow. Drive cars, dodge traffic and police, grab loot, and extract — all set in a procedurally built neon city.

## ▶️ Play it now

**[https://johannesrabauer.github.io/unity-mcp-test/](https://johannesrabauer.github.io/unity-mcp-test/)**

The game is built for WebGL and published to GitHub Pages — just open the link in a desktop browser.

![Neon City gameplay](Unity-ai-test/Assets/Screenshots/screenshot-20260628-070843.png)

## 🎮 Controls

| Action | Keyboard / Mouse | Gamepad |
| --- | --- | --- |
| Move | `WASD` / Arrow keys | Left stick |
| Aim | Mouse | Right stick |
| Shoot | Left mouse button | Right trigger |
| Reload | `R` | West button |
| Swap weapon | `1`–`4`, `Q`, scroll wheel | Shoulder buttons |
| Enter / exit car | `E` | South button |
| Jump (on foot) | `Space` | North button |
| Handbrake (driving) | `Space` | East button |

## 🕹️ Gameplay

- **Objective** — collect all the loot scattered across the city, then reach the extraction helipad.
- **Wanted system** — causing chaos raises a 0–5 star wanted level that draws police and decays over time.
- **Economy** — earn cash per pickup and per bust, with a time bonus for a fast clean run.
- **Arsenal** — four weapons, each with its own damage, range, spread, and ammo:
  - Pistol · SMG · Shotgun · Laser
- **Vehicles** — hop into cars to outrun the law; the city is alive with civilian traffic and pedestrians.
- **Health & respawn** — take damage, get wasted/busted, and respawn to keep the run going.

## 🏙️ The world

The city is generated at runtime by `CityBuilder` — a street grid of 16 blocks with sidewalks, lane markings, and zebra crossings, framing a civic downtown core (park, parking lot, hospital and police plazas) surrounded by neon high-rises and a far-corner extraction helipad.

## 🛠️ Tech

- **Engine:** Unity `6000.5.1f1`
- **Input:** Unity Input System (keyboard + mouse and gamepad)
- **Rendering:** Universal Render Pipeline (URP)
- **Target:** WebGL, deployed to GitHub Pages

## 📂 Project layout

```
Unity-ai-test/                  Unity project root
  Assets/
    Game/Scripts/               Gameplay systems (player, cars, AI, weapons, HUD…)
    Scenes/SampleScene.unity    Main scene
    Screenshots/                In-game captures
.github/workflows/
  deploy-webgl.yml              Builds WebGL and deploys to GitHub Pages
```

## 🚀 Building & deploying

The WebGL build and GitHub Pages deployment are handled by the
[`deploy-webgl.yml`](.github/workflows/deploy-webgl.yml) workflow (run manually via
**Actions → Build WebGL & Deploy to GitHub Pages → Run workflow**). It uses
[game-ci/unity-builder](https://github.com/game-ci/unity-builder) and requires the
following repository secrets:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

### Run locally

1. Install Unity `6000.5.1f1` (via Unity Hub).
2. Open the `Unity-ai-test` folder as a project.
3. Open `Assets/Scenes/SampleScene.unity` and press **Play**.
