# Saku Nurmiranta
**Home Assignment 1**

**Peliohjelmointi**  
**POKT21SP**  
**3.9.2025**

---

## Online game environments S25
**Anton Yrjönen**

---

## 1. A closer look at Player Unknown’s Battlegrounds (PUBG)

Player Unknown’s Battlegrounds (PUBG) is a multiplayer shooter game that works on a region-based Client-Server architecture. The rudimentary multi-player functionalities include:
- Matchmaking based on region, skill, game mode, and map choice.
- A lobbying system to separate matches into squad, duo, or solo matches.
- A system for private contact lists (friends) and invitations.
- Subscription-based access to customizable private lobbies that are instantiated to servers on demand.
- A kernel-level proprietary anti-cheat enforcement system called **Zakynthos**, which works in conjunction with **BattleEye**.

Other services provided include community features such as:
- Shared voice chat, text-based messaging.
- The ability for users to form greater bonds via clans.

The game is cross-platform compatible between console and PC players, and a separate version of the game is available for mobile devices.

Originally, the game was released as a subscription-based service. However, nowadays, the business model is based on three different levels of subscription access: **free**, **prime**, and **prime+**, as well as an in-game store that offers cosmetics for sale, such as:
- **Skins** (clothes, weapons, vehicles, parachutes).
- **Emotes** (poses and dances, etc).

The market system is closely tied to ongoing events, and a battle pass system has also been implemented with different levels of access tracked by in-game currencies:
- **BP (Battle Points)**.
- **UC (Unknown Cash)**.

Visually, the game positions itself as accessible to both casual players and the esports crowd. Rankings and leaderboards are available.

---

## 2. A brief overview of different network architectures and protocols

### **Architectures**

When talking about **server toplogy**, there are essentially two main types:
- **Client-Server Architecture**.
- **Peer-to-Peer Architecture**.

Anything else can be seen as related to or derived from these two, but solutions that implement aspects from both are called **hybrid models**.

#### **Client-Server Architecture**
In this architecture, the **client** connects to the **server**, which runs one instance of the game. In an **authoritative server model**, the clients use the server as a referee when individual game states on client machines face drift or desynchronization issues.

An authoritative server can run **headless** (without graphics) to keep track of the simulation and communicate the legitimate scenario to clients. This setup is called a **dedicated server**. The opposite is a **listening server**, which simultaneously runs the game as a client; this is often referred to as peer-hosted. However, games with listening servers are prone to exploitation by the host, as hosting players often act as system operators or game masters and may not participate in a "normal" capacity.

The downside of a **client-server architecture** is that each exchange between players requires the server acting as a middleman, leading to increased RTT (Round Trip Time), effectively doubling latency compared to peer-to-peer architecture. Server bandwidth demand grows in **linear fashion** (O(n)), but client requirements are more relaxed.

#### **Peer-to-Peer Architecture**
In this architecture, connections are made directly between peers, leading to exponential growth in connections (**O(n²)**). Bandwidth requirements are higher for clients compared to client-server architecture but are distributed symmetrically between peers.

Peer-to-peer architecture generally has **lower latency** due to the absence of a server as the middleman. However, synchronization is a challenge. Solutions often involve:
- Systems forcing the game into **“turns”** (deterministic lockstep model), where participants queue their actions, which are executed together after a brief grace period.

---

### **Protocols**

#### **TCP (Transmission Control Protocol)**
- A **reliable** but rigid protocol that ensures data arrives in order.
- Retransmits data until success, making it great for emails, chat messages, and turn-based games.
- Unsuitable for fast-paced, real-time multiplayer games because of lag and performance issues.

#### **UDP (User Datagram Protocol)**
- A **lightweight** and **customizable** protocol that offers no guarantees or interpretations.
- Delivers exactly what is sent but leaves memory allocation and interpretation to the application layer.
- Good for fast-paced games when exact packet orders are less critical.

---

## Usage of AI in this exercise

For the purposes of this set of exercises:
1. I used a **large language model** to create a master list of networking architectures and protocols.
2. I recursively used the model to focus on aspects specifically relevant to **online game programming**.
3. A **checklist** (size A4) was generated to serve as a roadmap for exploring these topics.

Before submitting the assignment, the same LLM was used to:
- Proofread.
- Cross-reference against the original task requirements.

This process reflects my usual approach to academic work: using AI as a **proofreader**, **editor**, and **assistant**, while reserving the task of learning and understanding for myself.