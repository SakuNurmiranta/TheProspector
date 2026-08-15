# Saku Nurmiranta
**Home Assignment 3**

**Peliohjelmointi**  
**POKT21SP**  
*1.10.2025*

---

## Online Game Environments S25
**Anton Yrjönen**

I used OpenAI’s large language model (LLM) as a study aid for this assignment, mainly for guidance on structure, referencing, and clarity; the analysis and conclusions are my own.

---

## Description of the Game

**Scandinavian Extreme Metal Manager '91 (SEMM91)** is a turn-based simulation of the emerging Norwegian Black Metal scene in the early 1990s. Each player controls a band leader, and each turn represents a season. Some bands are disqualified every year (four turns or one round) for a variety of reasons.

The goal is to gatekeep the scene and keep it **TRUE** by releasing metal albums that are acceptable to hardcore fans (the elite) but too extreme for regular fans (the posers). However:
- **Poser Acceptance**: Increases resources, but risks "selling out."
- **Elitist Acceptance**: Makes music harder to access, but brings players closer to victory.

**TRUE** is a composite value guiding the game:
- For example, in Round 1, `TRUE = "Brutal, Ugly, Cold"`, but in Round 3 it may evolve to `TRUE = "Brutal, Cold, Ugly OR Symphonic"`.
- Over time, the structure complicates, e.g., Round 5 might evolve into `TRUE = "Brutal, Cold, Ugly AND Fast OR Symphonic, SOMETIMES foot-fetishist."`

### Mechanics
- Band members and groupies act as **agents** to purify or gatekeep the scene through a variety of methods, legal or otherwise. Each agent has 2-3 actions per turn.
- Players also control an **avatar**, leading the band. Avatars are mechanically the most capable characters; however, using them in high-risk activities can potentially harm them.
- There are structures in the game world that radiate the **concept tags** that make up TRUE or their anti-values. Structures opposing TRUE become targets of hostile actions.

**Gameplay Dynamics**:
1. Conforming to TRUE earns elitist acceptance but increases difficulty.
2. Extreme actions gain notoriety, which attracts public attention for better or worse.

The player at the top at the end of the last season becomes the **Keeper** (if they outmaneuver the current Keeper).
- **Keeper Role**: Inspired by Oystein "Euronymous" Aarseth and his record shop, Helvete.
- The Keeper cannot produce material directly but gains credibility through others who follow their sound or align with their concept of TRUE.
- Other players must either align with the Keeper or find a way to overthrow them through a powerful maneuver.

Finally, SEMM91 also works heavily as a **social game** involving role-playing to influence outcomes and interactions.

---

## Specific Online Features

### Host Migration – "Game of Chairs"
- The role of the Keeper (who hosts the game) **changes mid-play** when certain criteria are met.
- Networking will also provide a **social interaction system**:
   - Players can only communicate if their Avatars are within proximity or engaged in the same activity.
   - This mechanic ensures dynamic social gameplay (i.e., the Keeper controls who communicates).

### Pariah Mechanics
- Players sent away by the Keeper (or those disobeying them) become **Pariahs**.
- Pariahs interact with other pariahs, creating an **underground faction** that may organize against the Keeper.
- Failure to cooperate often results in these players approaching a game-over state due to instability and isolation.

---

## Technology and Features

- **Engine**: Unity
- **Framework**: Netcode for GameObjects
   - Using **Distributed Authority Topology** for migratory hosting.
- **Plugins/Add-ons**:
   - Unity Transport
   - Multiplayer Tools
   - **ParrelSync**: This plugin allows the simulation of multiple instances of Unity during runtime for host/client testing (e.g., simulating poor network conditions).

---

### Current Progress
- The **Host Migration** system will likely be the most challenging to implement, particularly to handle:
   1. Migrating hosts during play.
   2. Handling dropouts (host or client).

**Proposed Solutions**:
- Introduce recovery points at the beginning of each turn (season) and round (year) to reduce disruption.

### Voice Chat
- Not yet implemented. Complexity unknown at this stage.

---

## Future Considerations

- **Authentication and Moderation**:
   - The game will include gamified moderation, requiring group efforts to handle abusive players (e.g., banning requires player consensus).

- **Lobby System**:
   - Players would define their **safe space** during lobbying, addressing potentially taboo topics:
      - Example topics: "Is hurting people ok?", "Cruelty to animals," or "Burning real estate."
   - Discussions and matchmaking occur **behind an alias**, emphasizing player safety.

---

### Current State of the Project
1. **Multiplayer Demo**:
   - Two Unity instances can form a **Client/Host relationship**.
   - Players "play the same game," where pressing `spacebar` advances turns and accumulates a generic `"influence trait."`
2. **Gameplay Logic**:
   - After 4 turns, the client sends its influence to the host for comparison.
   - A winner is declared (host or client).

However, the game is largely unstructured:
- Turns are not yet synced.
- Much of the implementation relies on generic, **AI-generated output**.

---

**Conclusion**:  
While 95% of the project is still conceptual, SEMM91 shows promise as an innovative social strategy game relying heavily on role-playing and dynamic interaction. With further development of host migration and other networking features, the project has significant potential for refinement and unique gameplay.