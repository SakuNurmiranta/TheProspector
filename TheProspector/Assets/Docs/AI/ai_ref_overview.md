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

There is always a special character in the game, called The Keeper. They carry immunity against claims of being a poser, because they have been so successful lately. 
The Keeper has a special advantage while they are ahead: running a record shop and a record label at the expense of the community. 
Other players manage their bands, trying to align closely with the Keeper only as far as it takes to force their own notion on what is considered to be True. Outside the Keeper and their Circle are the Pariahs, who have been fallen to disfavour. 
Their path is harder, because they lack the unity and resources of the circle, but they are also more free to do what they want with their music.

Every 4 turns, certain participants are dropped by either:
- Selling out (*breaking into the mainstream*).
- Fading into obscurity (*taking too many risks and alienating even the elite*).
- Retiring while still ahead (*avoiding the risk of producing an album that is no longer considered “true”*). A band must be considered already successful by the scene to retire this way.

All this game play is deeply interconnected, with separate flavors of True forming family trees, friendships forming between members of the Circle and beyond. And with the passing of each year, the struggle for dominance over what is True becomes harsher and more violent.

### Development Goals
This project exists to:
1. build on and demonstrate my understanding of a turn-based multiplayer game implementation using Unity Engine for a course (Online Game Environments S25). 
2. serve as a platform for the build up of certain mechanics that will be implemented in another project (The Prospector Roguelike)

### Key Features and Mechanics
1. A 2D board/card game aesthetic.
2. Turn based and in lockstep. No real-time events that would influence multiplayer game outcome. 

### Glossary
- **bulletin board:** a target for a promotional action
- **Circle, The:** A reference to both the group of players consisting of non-Pariahs, and the resources provided for the Circle (Keeper controlled). The Circle has a tremendous weight on what is considered to be True. 
- **clandestine actions:** Agent actions which bring notoriety to a player and/or the scene.
- **clout:** Credibility within the context of what is considered True. Staying True brings clout. Changing what is considered True brings a lot.
- **Demo(n) tape:** An album's raw version, recorded by the band itself, on a C-cassette. Condition is 100%. Every copy erodes the condition. 
- **Fading to obscurity:** A game over state where the player produces such an obscure record nobody can find it, or the record is otherwise 100% inaccessible.
- **Media, The:** A terrible force that misunderstands all the dark deeds of the Circle as something otherwise just as sinister but thematically non-True. Brings all the Posers in. Notoriety sells records, however.
- **Notoriety:** A trait which helps record sales and draws the Media in. There are three types: Personal, Circle and Scene Notoriety.
- **Outside, The:** Outside the Circle. Without an element originated from within the Circle, the Outside cannot produce anything considered True. Without reference to True, a thing cannot be Pure. 
- **Pariah, The:** A player who has been cast out by a Keeper Action, or Voted out by Regular Vote. A Pariah has complete artistic freedom, but has absolutely no backing from any safety nets.
- **promotion:** act of trying to inform the elites about a record
- **Pure:** Describes something that has no outside influence, something that is viable as itself. Purity is degraded by various mechanics in the game. 
- **retailer:** a place that sells records
- **SEMM91:** Scandinavian Extreme Metal Manager '91, the game title for this project.
- **selling out:** a game over state where the player's album has only traction amidst Posers.
- **social hub:** a place where the band members socialize beyond working with their music.
- **True/Tru/Trv(e)/...:** Describes something that fulfills a subliminal demand for authenticity, 
or adherence to an ideal standard. Often used to denote a steadfast commitment to core principles 
or values without compromise.
- **Wave, 1st:** The progenitors of what is considered True, on what The First Keeper based their artistic output.
- **Wave, 2nd:** The current output of the Scene, struggling to remain True and Pure.
- **Wave, 3rd:** A hypothetical damnable abomination, most likely to be created by a conspiracy between some Pariah, The Outside and The Media.

### Current Progress
- First rudimentary steps to define the game, flesh out the project and its structure, use cases, glossary, game loop and such (19-21.9.2025) WORK IN PROGRESS
- Activity and State Diagrams. 22.9.2025.

