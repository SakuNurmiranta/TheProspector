# Saku Nurmiranta
## Home Assignment 5

### Peliohjelmointi
**POKT21SP**  
**28.10.2025**

---

## Online Game Environments S25
**Anton Yrjönen**

---

### Network Test Outline

#### Test Cases
My goal is to create a personal and intimate, non-professional-looking DIY multiplayer implementation, reminiscent of a board game session between friends who already do this every week. This should only be a superficial, design-level choice, and the actual networking implementation should be robust and trustworthy for the end user.

The *Game of Chairs* idea, which I have described elsewhere, leads to the following test cases:
- Testing network failures of the current host with varying numbers of other players and how it influences the flow of the game (including the outcome of the game).
- For non-host players:
    - How does the game handle the case in which the only remaining regular player drops out?
    - How does a player enter the game mid-session?

---

#### My Approach
First, a caveat: My rationale for implementing these multiplayer aspects is because this allows me to later create bots to control other “players” and make the game into a single-player experience. Originally, I was only creating a multiplayer game because I was told to, which is now a good thing: it forced me to engage with an approach that enables AI implementation at a later stage. So, my networking efforts are not “organic” but influenced from the outside—much to my own benefit.

Here is what I’m going to do:
1. **Local Testing**
    - I’m going to test everything locally using **ParrelSync** and ad hoc, trying to stress-test the outlined test cases.
    - I’ll work with three iterations of the editor clone because this is the maximum number my physical setup can handle, limited by the screens I have available.

2. **Server Testing**
    - Since our studies include server creation, I plan to use that server for the next phase of tests.
    - This phase will essentially re-run the same tests, but via an actual network connection.
    - I’ll use a headless Linux PC collecting dust in the corner of our living room for this purpose.

**Important Considerations:**
- ParrelSync significantly screens latency, recovery rate, and other hardware separation-based testing scenarios because everything happens within the same system.
- To address this limitation, I will also use the **Multiplayer Tools** package during this stage.
    - This package lets me simulate differing network connection speeds and dropouts directly from the Unity editor window.

3. **Real Network Environment**
    - After moving to actual server testing, dropouts can be caused in real-world conditions and reduce the need for simulation.

---

#### Debugging and Gameplay
- Most testing steps will output data to the **debug log**, as metering is sufficient for this purpose.
- A polished gameplay experience is beyond this course's scope, so it is not a concern at the moment.

---

#### Bug Reporting
- As for bug tracking, I’ll likely maintain a **simple tally** in my project documentation unless a formal bug reporting system is required as a part of the course exercises.

---

### AI Blurb
A large language model was used to **proofread the text** and ensure it meets the task criteria as provided by the teacher. Writing essays is my strong suite, so I would never, in a million years, let a machine handle that for me.