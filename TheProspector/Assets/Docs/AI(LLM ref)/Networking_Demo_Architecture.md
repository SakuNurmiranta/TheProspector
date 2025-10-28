## REVISED DESIGN REFERENCE: Networking_Demo_Architecture

The Networking_Demo_Architecture is placed to set up the most rudimentary structures for game architecture. It needs to implement Unity's multiplayer functionalities, particularly the Distributed Authority architecture, which it is used to test mostly.

The demo scenario behaves as follows:

Use case: 
- Player starts the session with Distributed Authority logic, by providing their name, and the session name. 
- Two other players are expected to join to the same session (as per distributed authority logic). When two other players are found, the game starts.
- The game ends after 9 rounds (each made of 4 turns), and the player with the most points is declared winner.
- After each round, the accumulation of each player's points is saved within the instance. 
- The role of Keeper is determined randomly at the beginning between players. Other players are regular players. If the keeper is lost, the current round is cancelled at the end of its last turn, and the game is reverted back to the last saved instance. The new keeper is resolved from that last instance, and the missing player is discarded.
- The game loop is weighted so that one of the regular players always accumulate more points than the Keeper.
  - The exact logic for points is like this: First player gets 2 points, Second player gets 3 points, Keeper gets 1.
- After every 4 turns, the game checks who has accumulated the most score, and the one who has most is declared the new Keeper. The next round begins and the turn counter resets.
- If the keeper exits, one of the remaining players is declared the host, as stated earlier.
- After 9th round, the current Keeper calculates who won and informs other players. After player input on the winner, their instances are destroyed. Finally the current Keeper's instance is destroyed and the game is over.

All the demo needs to handle is the establishment of the network game between 3 players and handle the switching of hosting mid game. 

Revision:

This demo can be brute forced over the existing scene structure. The resolving of each round is done on the current Keeper, and the other instances are updated from how the current Keeper resolves the round.
A switch case is to be used to look up if the instance is that of the host (Keeper) or that of the player. 

As for the moment of implementation, Unity Netcode for Game Objects and Distributed Authority topology are already installed into the session. ParrelSync is also available, used to simulate multiple participants in the game.