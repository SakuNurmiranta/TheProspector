# Saku Nurmiranta
## Home Assignment 4

**Peliohjelmointi POKT21SP**  
**28.10.2025**

### Online Game Environments S25
Anton Yrjönen

---

### (Almost) Starting to Implement, or Implementing in Reverse

First things first: I have not implemented anything. Or to be precise, I **started to build a basic server-client**, utilizing **Netcode for Game Objects**. I reached a simple beginning scenario, where **ParrelSync clones** could mock-network a game where:

- One of them was a host.
- Two others were clients.

The system included the following managers:
- `gameManager`,
- `networkManager`,
- `inputManager`,
- `timeManager`.

**Gameplay scenario:**
- A player pushes a button and gets points.
- Every 4 turns (or four pushes per player), the score is compared between players on the host machine.
- A victor is declared.

However, this implementation was roughly done but built on **the wrong topology** for the next steps. The aim was to make use of **Distributed Authority**, which was required for **switching hosts** every time there was a victor.

After going back and forth and diving into tutorials (both **Netcode** and **Distributed Authority**, which both worked), I became confused and decided to **start fresh** instead of reverse-engineering the Distributed Authority tutorial into my earlier implementation.

---

### Current Status

I have come a long way to end up back at square one. However, that doesn’t mean I’ve achieved nothing. I have:

1. **Written 6 schematics** depicting the game systems (attached, where applicable).
2. **Scoped the project** into a suitable size for this course (and for another course I’m attending).
3. Developed a **vague understanding of what to do next**.

### System Classes Progress

To better understand how my game works, I’ve **fleshed out the most important classes** of my game. Classes marked with a **red overlay** are excluded from the first project iteration. This marking applies to schematics as well.

#### GameEntity Derived Objects
These engage with the **TRUE value** in the game, which is a central gameplay idea described in an earlier homework submission.

---

### Gameplay Concept

In the game, players interact with **track recorders** of different sizes. These recorders are used to:

- Implant their character’s **feelings and attitudes** into dictionary-style game objects called **tracks** and **albums**.

#### Points System:
- A **"demon tape"** is the product of player actions.
- Points are awarded based on the combination of **tags** and **aspects** imprinted on the tape.
- After players "listen" to another player’s tape:
    - Imprinted tags become available to their characters (if not previously owned).
    - Using another player’s tags causes **point bleed**—some of the points revert back to the originator of the tag.

---

### Components: Tags & Aspects

#### Tags
Tags are crucial gameplay elements shared among players after usage.

#### Aspects
Aspects consider elements that are **"kosher" to the scene** (e.g., instruments used or promo visuals like "corpse paint").

- **Novelty Factor:** Introducing **new aspects** in an approved manner results in **point multipliers**.
    - Example:
        - A **cold, ruthless guitar riff** may give 10 points.
        - A **cold, ruthless bagpipe part** (if well-executed) could earn 100 points. Failure with novelty introduces risks, such as **game-over scenarios**.

---

### Sequence Diagram

The following sequence diagram illustrates an **entire game session**:

- The **green box** indicates the **keeper** (who has Distributed Authority).
- The diagram describes what happens when the host/keeper experiences a **network failure** and later re-joins the game.

**Diagram not included in text**—the boxes contain snapshot numbers showing the **rounds in which the player’s progress is saved**.

---

### Reflection & Observations

Despite the **current lack of implementation**, I’ve invested heavily in planning for the project, which has a life of its own beyond this course. **Networking aspects** are the next major work item, and I estimate needing **1-2 weeks** at the time of writing to make significant progress.

#### Usability Issue in Unity Editor

During my exploration of the **Distributed Authority** system, I encountered an unintuitive issue in Unity:

- I was unable to switch to `DistributedAuthorityTransport` directly in the **Inspector dropdown menu** when it was currently set to `NetworkManager(Unity Transport)`.
- I had to:
    1. Deselect `NetworkManager(Unity Transport)` from the dropdown.
    2. Select `DistributedAuthorityTransport` from another menu.

This process felt very unintuitive. It may suggest room for improvement in Unity's **UX** for such cases, or it might simply be something to adapt to as a developer.

--- 