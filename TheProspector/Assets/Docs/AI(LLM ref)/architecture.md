## DESIGN REFERENCE: Architecture overview

**GameEntity family:** All objects that exist in the game world are derived from the GameEntity class. The gameplay consists of turn-based actions given to all "agents" under players control, agents being derived from the actor class (as derived from GameEntity class). Actors are derived from LocalizedEntity class. The player themselves have 
an avatar in the game, technically a controller type agent. Structures such as libraries, police stations or churches are also GameEntities, but they are not actors, belonging to the structure class instead. Structures deviate from the main branch of GameEntity inheritance at LocalizedEntity class.

**Turn structure:** The GameEntities exist in an updating game world, where 4 turns (spring, summer, fall, winter) make one round (or year). After each turn, there is a turn-resolution phase. After one round, there is a round-resolution phase. Resolution phases are calculated on host machine. The host is referred to as The Keeper. The hosting is not static but enabled to be migratory via Distributed Authority.

During a turn, each client builds "a bucket" of their agent actions to be delivered to the host after serialization. After deserialization in the host machine, all individual buckets are resolved into a single turn-timeline of events. The host calculates any potential conflict actions, basically resolving the turn outcome. 
Finally, when the turn outcome is resolved, the host keeps a tally of how each indivual player's year has gone this far, and the turn outcome gets updated to the clients. 
After the fourth turn is resolved, a round resolution phase ensues. The most important factors are only resolved at the round resolution. The host calculates round resolution based on the tally taken between turns.

**Networking features:** After each round is resolved, the player who gets the most points has the option to seize the title of the Keeper, thus also taking the hosting job for their machine. This should be available in Distributed Authority architecture.