# DEPRECATED DESIGN NOTE — DO NOT UPDATE

This document describes an early networking demo built to explore
Distributed Authority and host migration in Unity Netcode for GameObjects.

The architectural approach described here has been abandoned.

Reasons for deprecation:
- Host migration introduced unnecessary technical complexity
- Keeper (game authority) is now explicitly decoupled from host authority
- The demo logic no longer reflects the current or intended architecture

This document is preserved for historical reference only.


## REVISED DESIGN REFERENCE: Networking_Demo_Architecture

The Networking_Demo_Architecture is placed to set up the most rudimentary structures for game architecture. It needs to implement Unity's multiplayer functionalities, particularly the Distributed Authority architecture, which it is used to test mostly.

The demo scenario behaves as follows:

Use case: 
- Player starts the session with Distributed Authority logic, by providing their name, and the session name. 
- Two other players are expected to join to the same session (as per distributed authority logic). When two other players are found, the game starts.
- The game is simple. A player pushes space bar and receives a set number of points and furthers their local turn by 1. The number of points is first accumulated locally and determined if the player is currently a host or client. After turn counter is 3 (at the end of round 4), the points are no longer accumulated until the host resets the turn counter.
  - The exact logic for points is like this: First player gets 2 points, Second player gets 3 points, Keeper gets 1.
- Each round consists of 4 turns and the turn counter is nulled at the beginning of a new round. At the end of the round, the host/keeper calculates their own accumulation of points and waits rpcs from other players. The clients (or regular players) in turn send their accumulated points value to the current host. 
- Upon having recieved all of the players values, the keeper/host arranges individual player points values in descending order, and declares the one with most points as the new keeper/host. If the host changes Distributed Authority handles this. Then the next round starts with turn counter back at 0, and each player scores set to 0 too. 
- The game ends after 5 rounds (each made of 4 turns), and the player whom the Keeper calculates as the winner of the last round (or the "next" keeper), is declared winner.
- After each round, the accumulation of each player's points is saved within the instance. This save point is overwritten after every round, and it is used only as a recovery point if things go wrong.
- The role of Keeper/host is determined randomly at the beginning between players. Other players are regular players. If the keeper is lost (network failure, perhaps), the current round is cancelled at the end of its last turn, and the game is reverted back to the last saved instance. The new keeper is resolved from that last instance, and the missing player is discarded from the game.
- The game loop is weighted so that one of the regular players always accumulate more points than the Keeper.
 
- After every 4 turns, the game checks who has accumulated the most score, and the one who has most is declared the new Keeper. The next round begins and the turn counter resets.
- If the keeper exits, one of the remaining players is declared the host, as stated earlier.
- After 9th round, the current Keeper calculates who won and informs other players. After player input on the winner, their instances are destroyed. Finally the current Keeper's instance is destroyed and the game is over.

All the demo needs to handle is the establishment of the network game between 3 players and handle the switching of hosting mid game. 

Revision:

This demo can be brute forced over the existing scene structure. The resolving of each round is done on the current Keeper, and the other instances are updated from how the current Keeper resolves the round.
A switch case is to be used to look up if the instance is that of the host (Keeper) or that of the player. 

As for the moment of implementation, Unity Netcode for Game Objects and Distributed Authority topology are already installed into the session. ParrelSync is also available, used to simulate multiple participants in the game.