## AI REFERENCE: Project Overview
*This reference file's primary function is to serve as a copy/paste text block for the consumption of a large language model at the initialization phase of a conversation. 
It's secondary function is to serve as a generic document or journal on the overall progress of the project* 

## Rules and Context
- The LLM should aim to assist in achieving the development goals of the project.
- Avoid suggesting unrelated tools, methods, or practices unless specifically asked.
- Responses should remain concise, practical, and aligned to Unity Engine development.
- Context-specific vocabulary, such as game design terminology, is encouraged.
- Do not assume any knowledge not explicitly stated in this document (e.g., avoid assuming specific game genres beyond the description given).

### SEMM91 (SCANDINAVIAN EXTREME METAL MANAGER '91)

### **Game Concept**
A turn-based simulation of the emerging Norwegian Black Metal scene in the early 1990s, where each player controls a band leader and each turn represents a season, with some bands to be disqualified every year (4 turns).
The goal of the game is to gate keep the scene and keep it *“true”* by releasing metal albums that are only acceptable to **hardcore fans** (called *the elite*), but too extreme for **regular fans** (called *the posers*). 
Besides releasing albums, band members and groupies act as agents to keep the scene pure via various means, legal or otherwise. Each agent has 2 - 3 actions per turn.
The player who has come up top at the end of last season is considered the Keeper, if they manage to outmaneuver the Last Keeper away from the position. The Keeper has a handicap on producing new material, but has cult leader type tools in his disposal. 
Other players must either suck up to the current Keeper, or be prepared to make an extremely powerful (sonically or otherwise) maneuver beyond the Keeper's control.

Every 4 turns, certain participants are dropped by either:
- Selling out (*breaking into the mainstream*).
- Fading into obscurity (*taking too many risks and alienating even the elite*).
- Retiring while still ahead (*avoiding the risk of producing an album that is no longer considered “true”*). A band must be considered already successful by the scene to retire this way.

The most influential player (the one who scores highest with this seasons release) can choose to challenge the current keeper.

All this game play is deeply interconnected, with separate flavors of True forming family trees, friendships forming between members of the Circle and beyond. And with the passing of each year, the struggle for dominance over what is True becomes harsher and more violent.

### Development Goals
This project exists to:
1. build on and demonstrate my understanding of a turn-based multiplayer game implementation using Unity Engine for a course (Online Game Environments S25). 
2. serve as a platform for the build up of certain mechanics that will be implemented in another project (The Prospector Roguelike)

### Key Features and Mechanics
1. Turn based and in lockstep. No real-time events that would influence multiplayer game outcome.
2. Most calculations are done at host and then projected to clients. Deterministic systems not withstanding.
3. The rule of three. Two actions, three if you push it (with a cost).
4. Distributed Authority topology on Unity Netcode for GameObjects.


### Networking Demo Features:
- In the Networking Demo the only implemented feature is a rudimentary game loop where year changes after every 4th turn, one of the regulars wins and automatically forces a change of Keeper. Logging out as Keeper and Regular can also be tested.

### Current Progress
- First rudimentary steps to define the game, flesh out the project and its structure, use cases, glossary, game loop and such (19-21.9.2025)
- Activity and State Diagrams. 22.9.2025.
- Started to define GameEntities class and derivatives on paper. 25.9.2025
- I must create the very basic game loop for network gameplay using Distributed Authority. So start designing the thing.
- Created the turn/round and a simple controller. Introduced namespaces. 1.10.2025
