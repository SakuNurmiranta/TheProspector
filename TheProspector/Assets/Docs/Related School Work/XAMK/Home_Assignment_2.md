# Saku Nurmiranta
**Home Assignment 2**

**Peliohjelmointi**  
**POKT21SP**  
**14.9.2025**

---

## Online Game Environments S25
**Anton Yrjönen**

---

### Statement on AI Use
I used OpenAI’s large language model (LLM) as a study aid for this assignment, mainly for guidance on structure, referencing, and clarity; the analysis and conclusions are my own. A more comprehensive list of how AI was used is provided after the sources section.

---

## Fleshing out my online Unity game project’s networking features

### **Game Concept**
My online game concept is **Scandinavian Extreme Metal Manager ’91**. It’s a turn-based simulation of the emerging Norwegian Black Metal scene in the early 1990s, where each player controls a band leader.

The goal of the game is to gatekeep the scene and keep it *“true”* by releasing metal albums that are only acceptable to **hardcore fans** (called *the elite*), but too extreme for **regular fans** (called *the posers*). Besides releasing albums, band members and groupies act as agents to keep the scene pure via various means, legal or otherwise.

Each turn, certain participants are disqualified by either:
- Selling out (*breaking into the mainstream*).
- Fading into obscurity (*taking too many risks and alienating even the elite*).
- Retiring while still ahead (*avoiding the risk of producing an album that is no longer considered “true”*).

---

## Topology

Unity offers a **host migration pattern** called **Distributed Authority**, which is a networking topology for Unity Game Objects implemented via **Netcode** (*Unity Technologies 2025a*). This service operates via **Unity Cloud**, eliminating the need to set up and configure servers manually.

To use DISTRIBUTED AUTHORITY:
1. Install the **Netcode for GameObjects** package.
2. Use the **Multiplayer Services SDK** (*Unity Technologies 2025c*).

Comparisons to other models:
- **Client-Hosted Listen Server**:
    - **Security**: Lacks robust protection mechanisms.
    - **Synchronization**: Can lead to failures if poorly implemented.
    - **Usability**: Not suitable for high-performance games using predictive physics and sync.
- **Advantages of Distributed Authority**:
    - Provides hosting migration.
    - Handles transitions and disconnections gracefully.
    - Cost-effective to implement.

To manage participants, the game will likely use either **Unity Relay** or **Lobby** (*Unity Technologies 2025e*). **Lobby** appears more versatile, though my limited understanding means further study is needed before making a final choice.

---

## Session Management

**Distributed Authority** requires the use of a **dedicated client host**, known as the **session owner** (*Unity Technologies 2025b*). The session owner:
- Is automatically assigned to the first client that joins the session.
- Automatically transfers ownership to another client in case of disconnection.

**Key responsibilities of the session owner include**:
- Receiving each player’s **album outcome data** at the end of every round, along with the **social vote data** (*see **Social Aspects** below*).
- Comparing album outcomes with the data from a weighted social vote table.
- Determining which album was most successful in staying “true” and publishing the results.

---

## Management of Network-Replicated Objects and Their Properties

In **Distributed Authority**, gameplay objects are **owned and simulated client-side** (*Unity Technologies 2025b*). However, the workload distribution can be highly customized.

Key considerations for **Distributed Authority**:
- Since **physics simulation** is performed separately by each client, this architecture is unsuitable for games relying on physics as outcome determinants.
- In this project, physics computation and simultaneous events are unnecessary because:
    - **The game is turn-based.**
    - All events occur sequentially.

Thus, the downsides of Distributed Authority are largely irrelevant to this project.

---

## Social Aspects

This game includes a **social gameplay mechanic** inspired by games like *Werewolf* or *Mafia*. Players decide collectively which thematic aspects of extreme music are considered:
- **Legitimate (true)**.
- **Illegitimate (fake)**.

### Implementation:
1. All player choices are gathered on the **host client’s machine** using **RPC calls** (*Unity Technologies 2025d*).
2. The host cross-references player choices locally for matches.
3. In case of matches, **positive or negative modifiers** are applied to album outcomes.

The mechanic ensures player interaction directly impacts the results of each round, adding strategy and player-driven outcomes to the experience.

---

## Sources

1. Unity Technologies 2025a. **Netcode for GameObjects Documentation**: Distributed authority topologies. *Unity Technologies*. Available at: [Netcode Documentation](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.4/manual/terms-concepts/distributed-authority.html) [Accessed: 15 Sep, 2025].

2. Unity Technologies 2025b. **Netcode for GameObjects Documentation**: Ownership. *Unity Technologies.* Available at: [Netcode Ownership](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.4/manual/index.html) [Accessed: 15 Sep, 2025].

3. Unity Technologies 2025c. **Multiplayer Services Overview**. *Unity Technologies*. Available at: [Multiplayer Services Overview](https://docs.unity3d.com/6000.2/Documentation/Manual/multiplayer-overview.html) [Accessed: 15 Sep, 2025].

4. Unity Technologies 2025d. **Netcode for GameObjects Documentation**: RPCs. *Unity Technologies*. Available at: [Message System RPCs](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.4/manual/advanced-topics/message-system/rpc.html) [Accessed: 15 Sep, 2025].

5. Unity Technologies 2025e. **Relay vs Lobby**. *Unity Gaming Services Documentation*. Available at: [Relay vs Lobby](https://docs.unity.com/ugs/manual/relay/manual/relay-vs-lobby) [Accessed: 15 Sep, 2025].

---

## AI Use Disclosure

I hereby disclose that I used OpenAI’s **large language model (LLM)** to assist in the preparation of this assignment. The LLM’s role included:
- Clarification of assignment requirements.
- Guidance on selecting relevant aspects of Unity’s networking features.
- Assistance in identifying and formatting appropriate source references.
- Suggestions for structuring the essay and integrating in-text citations.
- Editorial feedback to improve clarity and academic tone.

### Principles:
All substantive content, analysis, and conclusions in the essay are **my own**. The LLM functioned solely as a study aid and writing assistant, not as the author of the work.

### Specific Contribution:
The only part of this submission written by the LLM is this disclosure.