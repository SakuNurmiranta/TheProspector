# Saku Nurmiranta
## Home Assignment 6

### Peliohjelmointi
**POKT21SP**  
**4.11.2025**

---

## Online Game Environments S25
**Anton Yrjönen**

---

### The Goal
Yesterday, I took control of the AWS LightSail server as instructed, tried SSH tunnelling, and uploaded files there. These were familiar tasks for me, as I’ve completed a Linux beginners server course in the past. All these tasks were relatively simple, but I encountered a (stupid) hiccup when I assumed the AWS-side server was premade and ready to accept calls through the firewall. After some confusion, I realized that the instructions for firewall exceptions needed to be applied on **both** the local machine and the Amazon server (located in Stockholm, Sweden).

The files I uploaded to the server were for an easy-to-implement Minecraft clone called “Luanti.” I was required to install the game itself and provide the scenario on the server. Unfortunately, I never got the game to work. The issue? The version of LightSail server I used has only 2GB of memory, and, according to my research, a Windows Server requires most of that just for idling, leaving nearly no processing power for anything with a 3D engine. You warned about this scenario, and I had to learn that the hard way.

---

### My Approach
Today, I decided not to mess with the Minecraft clone because I’m supposed to focus on a project of my own anyway. Quite a lot rides on this project, and I’m using it as a playground to gain at least 16 ECTS or more.

Here is what I worked on:

1. **Local Host System Implementation**
    - I managed to implement a local host system, which also supports the **Distributed Authority** system I’ve been discussing for the past month.
    - The regular host-client system worked flawlessly.

2. **Distributed Authority System**
    - This system allows for **host migration**, but it isn’t fully implemented yet.
    - I created a simple **UI** that lets players choose between two options:
        - The host-client topology.
        - If the Distributed Authority system fails at startup, it automatically falls back to the host-client topology.

3. **Network Managers**
    - I had difficulty implementing two network managers because they are most logically handled using singleton patterns.
    - To address this, I created a “Net-bootstrap” script that ensures only one network manager is active based on user selection.
    - Additionally, I used **RELAY** in the implementation.

4. **GameCoordinator Script**
    - The game logic is currently managed by a **GameCoordinator** script, which is spawned into the scene only after the underlying topology selection is made.
    - The system works with local clones, and the mechanism for host migration triggers at the expected moments, but the actual implementation of the migration isn’t complete yet.

---

### Future Work and Challenges
At this point, I must admit that my implementation uses a significant amount of **AI-generated scripts**, and I anticipate needing to clean it up later. Over the past few years, my skills have primarily focused on **game design**, which leaves me frequently struggling with the more technical aspects of this course.  