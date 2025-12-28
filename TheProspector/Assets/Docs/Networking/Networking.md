## **NETWORK DESIGN REFERENCE**
*This reference file is here to keep track of the networking aspects of the project in general*

**Network related scripts**

Networking Manager: Manages all networking related activities in the game, similarly to the way gameManager tales central role of the basic mechanics. 
This class interfaces with the GameManager instance via to facilitate mechanics and outcomes. 
Like the GameManager class, this class implements the singleton pattern, and it overrides 
the OnNetworkSpawn() provided by NetworkBehaviour base class. 