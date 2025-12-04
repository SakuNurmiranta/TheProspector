# Building Testing Tools and Metrics for Your Test Environment

Consider how different aspects of your test environment can be measured and monitored effectively.

## Network Traffic Monitoring

### Metrics to Consider:
- **Ping / RTT (Round-Trip Time)**
- **Jitter**
- **Bandwidth**

### How to Monitor:
- Using a **separate tool**.
- Leveraging the **game or engine’s own statistics**.
- Implementing **custom solutions**.
- Monitoring can be:
    - **Static**: Measured/observed once.
    - **Dynamic**: Continuous monitoring during gameplay.

## Network Latency Simulation

### How to Simulate Latency:
- Use a **separate tool**.
- Utilize **game engine tools**.
- Implement **in-game coded latency simulation** or adjust **server environment settings**.
- Configure/mod **custom routers**.

### Key Questions:
- **What parameters can be modified and how?**
- **Can settings be changed on the fly** during a game session in the simulation?

---

## Reviewing and Updating Test Plan

- Revisit last week’s homework (**Test Plan**) and update it if necessary.
- Modify existing test cases or create new ones as needed.

---

## Performing Tests and Collecting Data

- Conduct the tests defined in your plan.
- Collect data during testing.
- Fill in test reports with the results.

## Notes:

- External tools (Windows): 
  - Command Prompt: Ping <server-ip>
    - 11-23 ms, average 11 ms (after 100 tests)
  - Powershell: Test-Connection -ComputerName <server-ip>
     - 11-19 ms, average 12 ms (after 10 tests)
  - Task Manager -> Performance -> Ethernet/Wi-fi (send/receive Kbps) 
    - Test: approx 55s with 3 clients to remote pacified host ("a player" host declared inactive in code)
    - Server type was a headless Unity build on remote Windows Server (a static Amazon Lightsail IP @ Stockholm)
    - Results: 
      - Baseline bandwidth was approx 8Kbps
      - Five spikes (72, 88, 96, 72, 64) coinciding with change of round (a four turn cycle and associated events)
      - Minor spikes (15, 24, 40, 40, 15) periodic resync and rpc calls
      - Nothing abnormal detected, bandwith usage is stable, and reflects player activy in-game
    - Jitter Analysis:
      - method: cmd: ping <ip> -t, one client just sending advance turn commands to server
      - raw numbers (RTT, ms):
        - 12,11,12,12,11,11,11,11,11,11,11,11,12,11,11,11,11,11,11,11,11,
          11,12,11,11,11,10,11,34,11,11,12,11,10,11,11,11,11,11,12,11,11,
          11,12,11,11,11,11,11,13,11,10,10,11,11,11,11,11,11,11,11,11,18,
          12,11,11,11,11,11,13,13,11,11,11,10,12,11,14,11,13,11,11,11,10,
          11,11
      - minimum: 10ms
      - maximum: 34ms
      - jitter: 34ms - 10ms = 24ms
      - common value-range: 10-13ms
      - conclusion: a stable connection, low latency, no packet loss detected
    - Unity Multiplayer Tools -> Network Simulation tests
      - Tested with a simulated 2G network, along with other presets such as "Home broadband with congested network". Connection was editor-client to server in Lightsail. 
      - Reporting in each case is redundant, for the simulation simply reflects the desired network-settings which are superimposed over the actual connection.


- Issue: I need to make the server run headlessly, because each turn requires a remote action from my client computer. (resolved)
