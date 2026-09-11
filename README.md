# Afterglow

An AR mystery investigation game where clues, ghosts, and a diegetic narrator turn your real room into a crime scene.

## What it is

Afterglow is a mobile augmented reality mystery game built for my thesis project. Players scan their physical space to anchor evidence, spectral figures, and story beats into the room around them, piecing together a narrative through exploration rather than menus and cutscenes. The game was built as part of a research study into diegetic (in-world) narration and hint delivery in AR — how much can a game world *tell* the player without ever showing a UI prompt.

## Key features

- **Spatial evidence system** — clues are spawned and anchored to real-world surfaces via AR Foundation plane/anchor detection, manipulated and inspected in place.
- **Diegetic narration** — an in-world text-to-speech narrator responds to player context instead of relying on subtitles or menus.
- **Ghost silhouettes & signal escalation** — spectral visuals and audio cues that ramp up as the player gets closer to the truth.
- **Idle hint system** — nudges players who get stuck without breaking immersion.
- **Save/world state system** — tracks investigation progress and world state across sessions.
- **Study logging** — instrumented for the accompanying thesis user study.

## Tech stack

- **Engine:** Unity 2022.3 LTS
- **AR:** AR Foundation, ARCore (Android) & ARKit (iOS), XR Interaction Toolkit
- **Language:** C#

## Getting this project running

This repo contains the original code, scenes, and custom assets. It excludes a small number of **purchased Unity Asset Store packs** (their licenses don't permit redistribution) — to open the project in Unity, import these separately from the Asset Store first:

- `BK_AlchemistHouse`
- `AlchemyLabProps`
- `Multistory Dungeons 2`

## Screenshots / gameplay

_TODO: add screenshots and a gameplay GIF here._
