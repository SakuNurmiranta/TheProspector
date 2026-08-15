## DESIGN REFERENCE: Architecture Overview

### GameEntity Family

All objects that exist in the game world derive from the **`GameEntity`** base class.

Gameplay consists of turn-based actions performed by **agents** under player control. These agents are represented by the **`Actor`** class, which derives from **`LocalizedEntity`**, which in turn derives from `GameEntity`.

The player is represented in the game world by an avatar, technically a **controller-type Actor**, responsible for selecting collective and character actions.

**Structures** such as libraries, police stations, or churches are also `GameEntity` instances.  
They derive from `LocalizedEntity` but **do not derive from `Actor`**, as they do not perform actions independently. Structures therefore branch from the `GameEntity` inheritance tree at the `LocalizedEntity` level.

This hierarchy separates:
- existence (`GameEntity`),
- spatial presence (`LocalizedEntity`),
- agency (`Actor`).

---

### Turn Structure

GameEntities exist in an updating game world structured into discrete time units.

- Four **turns** (spring, summer, fall, winter) form one **round** (or year).
- Each turn concludes with a **turn resolution phase**.
- Each round concludes with a **round resolution phase**, where higher-level outcomes are evaluated.

Resolution phases are calculated by a **fixed authoritative host**.  
The host is a technical authority only and does **not** change during gameplay.

---

### Turn Execution and Resolution

During a turn:

1. Each client assembles a **bucket of actions** for all agents under their control.
2. Action buckets are serialized and sent to the host.
3. The host deserializes all buckets and merges them into a single **turn timeline**.
4. Conflicts and interactions between actions are resolved centrally by the host.
5. The resolved turn outcome is broadcast back to all clients.

The host maintains a **per-player tally** tracking round-relevant factors across turns.

After the fourth turn:
- A **round resolution phase** is executed.
- Only systems explicitly scoped to round resolution are processed at this stage.
- Round resolution is calculated based on the accumulated per-turn tallies.

---

### Networking Features

- The host remains constant for the duration of a session.
- The **Keeper** is a **purely game-level role** with no networking responsibilities.
- Keeper authority affects rule interpretation, legitimacy, and scene influence, but not simulation execution.
- All network synchronization, validation, and resolution remain under host control.

---

### Architectural Principle

**Game authority and simulation authority are intentionally decoupled.**
