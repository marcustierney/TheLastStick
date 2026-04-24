# Guidelines for AI agents

## What “AI” means here

- **Model-based generative AI**: learned systems (diffusion, LLMs for creative text, GANs, prompt-to-image/audio/voice/3D, generative inpaint/upscale that invents detail). Disallowed for **player-facing** creative output.
- **LLM as dev tool**: in-editor coding help. Allowed for **engineering** only—not for shipped story, dialogue, marketing, or other player-visible copy unless owners explicitly allow it.
- **Procedural generation**: hand-authored **code + data** (noise, seeds, grammars, rules, simulation, classical DSP). Allowed. No step should be “a generative model invents this asset.”

**Split test:** could this run without any trained neural **generative** model—only code, tables, and human-made sources? Then it is procedural/tooling, not banned generative AI.

## Human-made game content

Player-facing art, animation, audio, writing, and level **design** must be human-made (or licensed human-made third-party). Do not propose or replace assets with model-based generative pipelines—including AI VO/music, LLM-written flavor text for ship, or model-driven layout/set dressing as the main author.

Purely **algorithmic** layout (seeded PCG, constraints) is fine; see below.

## Procedural / automation (allowed)

Algorithmic, repeatable systems built from human rules and inputs, for example: noise/graph/grammar-based worlds and loot; kitbashing **non–AI-generated** parts; shaders and particles without a generative model authoring the look; procedural audio via classical synthesis/DSP; editor PCG scripts (same seed → same result).

## Dev-only AI

LLM assistance for **code** (gameplay, engine, refactors, tests, editor tooling), **implementing** procedural systems (you write code; humans own shipped creative quality), and build/CI/config. Internal comments, commits, and team docs are fine.

**Unsure?** Treat ambiguous **game-facing** output as generative AI and do not recommend it. For **code-only** work, stay within the dev-only rules.

**Edge cases:** gameplay/tech ML or new ML dependencies—get explicit human approval first. Photogrammetry, mocap, and normal denoise/repair are not “generative AI” in this sense; third-party assets must still be human-made and licensed—no generative-AI marketplace as a loophole unless approved.