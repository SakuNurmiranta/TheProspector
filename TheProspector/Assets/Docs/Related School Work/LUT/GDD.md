# **GAME DESIGN DOCUMENT: Demo(n)Tapes**

**Saku Nurmiranta, 003712926**

---

## **Short summary of applied tools and link to Project 1 video**

**Tools used:**
- **Engine:** Unity 6000.2.6f1
- **Language:** C#
- **Programming environment:** JetBrains Rider
- **Version Control:** GitHub Desktop + Git
- **Graphics:** Krita
- **Audio:** FMOD Studio, Reason Studios Reason 13.x + Reaper, arsenal of VST plugins, basic band instruments, some other instruments
- **Design assistance:** ChatGPT (OpenAI GPT-5)
- **Code/Documentation analysis:** JetBrains AI Assistant (within Rider IDE)

**Link to Project 1 explanation video:**  
[Link to video here]

---

# **Game Design Document**

### **Instructions**
- Define your own game idea with **1-3 sentences per point**.
- Consider the **publishable** aspects of the developed game.
- **Focus** on aspects 1-4, and define the remaining points generally since this is an introductory course to game development.
- Main aspects to define:
    - **Visual style:** Concept art or sketches for levels/characters.
    - **Player roles/abilities:** What the player does in the game.
    - **Technological requirements:** What technical elements need solving.
    - **Interface/control scheme design.**

---

## **Index**
1. **Executive Summary, Quick overview**
2. **Target Audience**
3. **Main Characters**
4. **Main Features**
    - 4.1 Core Gameplay Loop
    - 4.2 Game Entity Characters, Operative Agencies, and Character Actions
    - 4.3 Aspects, Ideas, and Conveyance
    - 4.4 Tags (Emotional/Ideological systems)
    - 4.5 Tag-Pairs and Genre-Specific Systems
    - 4.6 TRUE vs true, Novelty Factor, and Gravity Factor
    - 4.7 Track Construction and Recording
    - 4.8 Band and Collective Systems
    - 4.9 Multiplayer Mode
5. **Genre, Setting, and Concept Art Book***
6. **Enemies, NPCs, and Other Objects**
7. **Storyboard and Script***
    - 7.1 Story Overview
    - 7.2 Progression (World 1)
    - 7.9 Progression (World 2…)
8. **Technical Definitions, Tech Guide***
    - 8.1 Platforms, Versions
    - 8.2 Control Scheme
    - 8.3 Limitations
9. **Business Definitions***
    - 9.1 In-app Purchases
    - 9.2 DLC Packs
10. **Outsourced/Bought Assets**
11. **Technical Terms**

---

## **1. Executive Summary, Quick Overview**

**Demo(n)Tapes** is both a **game and an art project** inspired by the second wave black metal scene of the early nineties. Set in the fictitious Nordic amalgamation **Snordenmark (SNO)**, a land located in the Atlantic (near Iceland), the game reflects the **DIY spirit** and **anti-establishment ideology** of the black metal scene.

Each player controls a **band leader visionaire** who manages their own black metal band amidst a larger social circle of metal fans. Players navigate **years, seasons, or turns**, balancing aspects such as **elite credibility** and **commercial success** in an effort to foster music considered **TRUE (authentically black metal)** and avoid being labelled a **poser**. Players vie for control of the **record shop**, while preparing for the **cataclysmic end-game event** that concludes the game.

**Game Prototype:**  
Demo(n)Tapes serves as a **prototype for Scandinavian Extreme Metal Manager ’91 (SEMM91)**, a planned single-player campaign with expanded elements including record labels, live tours, and additional narrative depth. As it stands, Demo(n)Tapes is **online multiplayer only**.

---

## **2. Target Audience**

**Demo(n)Tapes** appeals to **middle-aged casual gamers** who resonate with its **underground dark humor** and **niche aesthetic**. It targets gamers already familiar with the **black metal subculture** or those intrigued by its **unconventional atmosphere**.

While limited in audience size, the project’s **artistic focus** prioritizes authenticity over mass-market success. For **metal enthusiasts** who recognize visual nods to bands like Darkthrone or Cannibal Corpse, the game’s aesthetic provides an almost irresistible draw. Given its current **free-to-play status**, it primarily relies on word-of-mouth appeal.

**Game Complexity:**  
Demo(n)Tapes emphasizes **systemic simulations** over linear progressions, exploring **ideological dynamics**, **power struggles**, and **scarcity** rather than content-focused gameplay.

---

## **3. Main Characters**

### **Nullsvorn (Default Player Character)**
- **Band Name:** "Not_Saved.SNO"
- **Stage Name:** Nullsvorn (editable)
- **Background:** Nullsvorn and their band "Not_Saved.SNO" began as **thrash metal cover artists**, evolving later into an **experimental black metal group** with an emphasis on **True Nordic aesthetics**.

### **Band Members**
1. **Nullsvorn:** Vocalist/Lyricist.
2. **Sithrogürd:** Bassist, also an *arsonist*.
3. **Keiser:** Guitarist.
4. **Molotov:** Drummer/keyboardist.

Each character has **aspects** and **tags**, which contribute to tracks and influence gameplay through **resonance, mood, and convictions** (see Chapter 4).

---

## **4. Main Features**

### **4.1 Core Gameplay Loop**
Gameplay cycles through **years (rounds)**, divided into **four seasons (turns)**. Each season, players perform:
1. **Collective Actions (e.g., gestation, rehearsal).**
2. **Character Actions** (dependent on the chosen collective action).

During turns, players must:
- Utilize **aspects** and **tags** to create **ideas**, which are arranged into tracks.
- Manage resources to balance between **TRUE output (street credibility)** and **poser-like cash grabs**.
- Promote music, perform gigs, and counteract other players’ sabotage.

At year’s end, **progress is evaluated**, and the dominant player becomes the **Keeper**, gaining temporary control of the **record shop** and the ability to shape scene dynamics.

### **4.2 Game Entity Characters & Character Actions**
- **Controllers:** High-agency characters; lead collectives (e.g., Nullsvorn as a bandleader).
- **Actors:** Semi-autonomous members of collectives.
- **Agents:** Augment gameplay with specific expertise (e.g., Sithrogürd’s *arsonist* tag).
- **Groupies:** Attach/stat-boosting characters linked to another entity.

### **4.3 Tags (Emotional/Ideological Systems)**
Tags are **core thematic markers** in Demo(n)Tapes, determining the overall **theme and impact** of tracks. Tags are categorized as:
1. **Resonance Tags:** Long-term identity tags.
2. **Mood Tags:** Temporary emotional boosts.
3. **Conviction Tags:** Reflect belief systems (e.g., Nullsvorn’s *pagan* conviction).

Each tag type plays a role in **track construction** and the creation of **TRUE re-sacralising tag-pairs**.

### **4.4 Tag-Pairs and Genre Systems**
**Tag-Pairs** are combinations that define **genre authenticity** (TRUE):
- **Re-sacralising Tag-Pair:** Combines opposing tags like *pagan* and *organized religion*.
- Failure to include a TRUE tag pair disqualifies a track from being **blackened** unless Keeper-approved.

### **4.5 Novelty Factor, Gravity, and Conveyance**
- **Novelty Factor:** First use of an aspect garners bonus influence.
- **Gravity:** Pull exerted by TRUE tags to shape the scene’s dynamics.
- **Conveyance:** Clarity of intent, determining how well ideas and tracks are received.

---

## **5. Genre, Setting, Concept Art Book***
(*Details extendable to separate document*)

---

## **6. Enemies, NPCs, and Other Objects**

---

## **7–11: Technical, Business, and Expanded Documentation**

### **Technical Terms**
| **Term**              | **Definition**                                                                 |
|-----------------------|-----------------------------------------------------------------------------|
| **Demo(n)Tapes**       | Game simulating the 2nd-wave Nordic black metal scene.                     |
| **Snordenmark (SNO)**  | Fictional Nordic country where the game unfolds.                           |
| **Keeper**             | Player with scene authority, controlling the record shop.                  |
| **TRUE**               | Authentically blackened music defined by re-sacralising tag-pairs.         |
| **Transient Tag**      | Temporary tag from interactions; must be resolved quickly.                 |
| **In-season Tag**      | Tags tied to current season (e.g., *morbid* in autumn).                    |
| **Tag-Pair**           | Combination creating thematic depth or TRUE authenticity.                 |
| **2:3 Rule**           | Characters can perform up to 3 actions, though the third induces penalties.|

(*Note: Some sections marked by (*) can be summarized or extended based on project requirements.*)