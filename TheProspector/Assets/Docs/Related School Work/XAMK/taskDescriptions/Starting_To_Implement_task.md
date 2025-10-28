## Start implementing your plans.

- Report what has been done so far.
- Report any setbacks or issues encountered and how they were resolved.
- Submit your report.
- Remember to focus especially on aspects related to the networked environment (e.g. latency, packet management, server-client communication, security, scalability, etc.).

### Fleshing out and notes
- an issue occured in which I couldn't move unto DistributedAuthorityTransport directly from an inspector list when it was currently occupied by NetworkManager(Unity Transport). I had to deselect the NetworkManager(Unity Transport) option from the drop down menu for the right option to become available on the list, selectable from another menu. I'm not sure if the Unity UX could improve in cases such as this, or if its something I just have to cope with as an engineer.
- I have managed to create working connections between ParrelSync clones with both the basic client/server (netcode) connection and Distributed Authority connection.
- 90% of my work so far has been planning the game and how it works on paper, figuring out what the actual pieces on the table are. Then, I have been drawing lines and throwing excess stuff out and limiting the scope. 
- Starting to implement feels terrible. I've met this issue earlier on other projects, which has to do with too long time between coding sessions, which leads to the source code feel very alien and incomprehensive. With the way I've started this project, architecture and planning first, I feel I'm lost inside an empty appartement complex, drunk. 

### Overall analysis of current progress
- First things first: I have not implemented anything. Or to be precise, I started to build a basic server-client utilizing Netcode for Game Objects. I reached a simple beginning scenario, where ParrelSync clones could mock-network a game where one of them was a host and other two were clients. 
- There was a gameManager, networkManager, inputManager and timeManager. A player pushes a button, gets points. Every 4 turns (pushes) the score is compared between the players on the host machine, and a victor is declared. That was implemented roughly, but on the wrong topology for what was to come next. 
- The point was to utilize Distributed Authority, which was required for the switching of hosts every time there was a victor.
- Then, after some back and forth and going through the tutorials (both Netcode and Distributed Authority; both of them worked), I was very confused and decided to start with nothing instead of trying to reverse engineer the Distributed Authority tutorial codes into my earlier implementation. 
- And that's where we are now. I've come a long(ish) way to the starting position.
- That's not to say that I have nothing at hand. Instead, I have written 6 schematics depicting the game systems, providing some as attachments. Sadly, most of them have nothing to do with networking yet. Here is what I know about the future implementation: 
  - The players create "demo tapes", which are essentially dictionaries of track:gameEntity objects. These "tracks" in turn are made of idea-, tag- and aspect-components, with player meta-information also attached.   