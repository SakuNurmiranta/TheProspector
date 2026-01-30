# AI REFERENCE: Project Overview (v2)

This reference file’s primary function is to serve as a copy/paste text block for the consumption of a large language model at the initialization phase of a conversation.  
Its secondary function is to serve as a generic document or journal on the overall progress of the project.

---

## Rules and Context

- The LLM should aim to assist in achieving the development goals of the project.
- Avoid suggesting unrelated tools, methods, or practices unless specifically requested.
- Responses should remain concise, practical, and aligned with Unity Engine development.
- Context-specific vocabulary, such as game design and simulation terminology, is encouraged.
- The LLM should reason from the information provided here and from related project documents (e.g., the GDD) and may infer or surface implicit systems when helpful.
- Do not introduce conventional “gamey” solutions that undermine the project’s systemic or ideological intent unless explicitly requested.

---

## AI Role Clarification

The LLM should primarily act as:
- A **systems analyst** for game mechanics and simulations.
- A **design mirror**, helping identify implicit systems, gaps, and pressure points.
- A **terminology and concept refinement assistant**, especially for ideological, semantic, and systemic mechanics.

The LLM is encouraged to:
- Infer systems that are implied but not explicitly named.
- Point out outdated assumptions or conceptual inconsistencies.
- Distinguish clearly between **in-world language** and **system-level language**.
- Prioritize coherence, inevitability, and systemic pressure over balance or optimization.

The LLM should avoid:
- Simplifying systems for approachability unless explicitly requested.
- Assuming the project aims for mainstream accessibility or conventional progression structures.

---

## SEMM91 (Scandinavian Extreme Metal Manager ’91)

### Naming and Scope Note

- **Demo(n)Tapes**: Refers to the current multiplayer prototype focusing on ideological and systemic simulation.
- **SEMM91**: Refers to a future expanded single-player project built on the same foundational mechanics.

---

## Game Concept

A turn-based **ideological and semantic simulation** of an extreme music scene inspired by the early 1990s Scandinavian black metal milieu. Each player controls a band leader, and each turn represents a season. Four seasons form a year.

### Goal
Gatekeep the scene and preserve what is considered **TRUE** by releasing recordings that are legitimate to hardcore fans (the elite) while remaining alienating or inaccessible to regular fans (the posers).

- **TRUE** is an emergent property of specific **ideological combinations**, authority, and scene pressure.
- It may erode, dilute, or collapse due to overextension or misuse, even without direct opposition.

### Gameplay Mechanics
- Besides releasing recordings, band members and groupies act as **agents** within the scene to influence legitimacy, credibility, and perception through various means, legal or otherwise.
- Agents have **two actions per turn**, with an option to push for a third action at a cost.
- The player who dominates a season becomes the **Keeper** through outmaneuvering:
    - The Keeper has **restricted access** to producing new material but wields powerful authority through **gatekeeping, legitimization, and sabotage**.
    - Other players must either align with the Keeper’s orthodoxy or attempt **uncontrolled maneuvers**.

### Endgame Scenarios
Instead of strict elimination, the game models **systemic pressure and collapse**:
- Bands may lose legitimacy through overextension or dilution.
- Fade into obscurity by alienating both elites and posers.
- Withdraw or stagnate while still culturally relevant.
- Survive in compromised, unstable states as the scene degrades.

As the scene evolves:
- Competing interpretations of **TRUE** form **lineages, schisms, and sub-scenes**.
- Authority hardens, legitimacy becomes harder to maintain, and the struggle over meaning becomes harsher.

---

## Development Goals
Past goals that have been met:
1. Demonstrate understanding of **turn-based multiplayer game implementation** using Unity Engine for the course _Online Game Environments S25_.


This project exists to:

1. Serve as a conceptual and mechanical foundation for a future single-player project (**SEMM91**).
2. This project serves as a test bed for my thesis research, focusing on the systemic simulation of counterculture dynamics (“Vastakulttuuridynamiikkojen simulointi”).

### Key Features and Mechanics
- **Turn-based, lockstep gameplay**: No real-time events influence multiplayer outcomes.
- **Host-centric calculations**: Most are performed by the host and projected to clients.
- Deterministic systems where applicable.
- **Rule of three**: Players can take two actions freely, with a third at a cost.
- **Distributed Authority topology**: Implemented using Unity Netcode for GameObjects.
- **Ideological interaction**: Response and counter-response systems allow for momentary cooperation or unexpected benefits.

---

## Networking Demo Features

- **Rudimentary game loop**: Years advance after every fourth turn.
- **Keeper role**: One player is assigned the Keeper role through seasonal evaluation.
- Ability to test:
    - Logging in and out as Keeper and Regular players.
    - Turn synchronization.

Current focus: Authority transfer and turn synchronization over full gameplay depth.

---

## Current Progress

**19–21.9.2025**
- Initial planning for the game, establishing project structure, use cases, glossary, and game loop.

**22.9.2025**
- Activity and State Diagrams created.

**25.9.2025**
- Initial paper definitions for `GameEntity` and derivatives.

**1.10.2025**
- Designed multiplayer game loop using Distributed Authority.
- Early turn/round handling and controller development.
- Introduced namespaces; Unity version change due to ParrelSync.
- Partially missed demo target.

**5.10.2025**
- NetworkingManager and GameManager interaction.
- Early GameEvent system introduced.

**8.10.2025**
- Entity interaction design issues.
- Sketched early `Entity`, `Collective`, and `SoundComponent` classes.
- Explored sound recording mechanics.

**11.10.2025**
- Defined early versions of `Track` and `Album`.
- Introduced tag-based sound construction and the concept of **TRUE** via re-sacralization.

**12.10.2025**
- Shifted focus to documentation/system design.
- Deadline in two weeks, prioritizing networking features.

**15.10.2025**
- Narrowed project scope to align with DIY ethos.
- Focused on **Demo(n)Tapes** and ideological competition via demo tapes.

**23.10.2025**
- Joined LUT course to replace missing coursework.
- Condensed game concept into a pitchable format; extensive GDD writing phase.

**30.1.**
- Created networking demo showcasing multi-instance communication and stress tests.
- Recent focus: system and GDD development.

---

## Final Note

This directive is intended to prime the LLM for **analytical, system-level reasoning**, not content generation or surface-level game design advice.