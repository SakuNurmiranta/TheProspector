## DESIGN REFERENCE: Networking_Demo_Architecture

The Networking_Demo_Architecture is placed to set up the most rudimentary structures for game architecture. It needs to implement Unity's multiplayer functionalities, particularly the Distributed Authority architecture, which it is used to test mostly.

The demo describes as follows.

Use case: 
- Player starts the game. It looks for two other players. When two other players are found, the game starts.
- Player 1 starts as the Keeper/host. Other players are regular players.
- The mock up game loop is weighted so that one of the regular players always accumulate more points than the Keeper.
- After every 4 turns, the game forces a change of keeper and thus a host migration.
- If the keeper exits, one of the remaining players is the host.

All the demo needs to handle is the establishment of the network game between 3 players and handle the switching of hosting mid game. 