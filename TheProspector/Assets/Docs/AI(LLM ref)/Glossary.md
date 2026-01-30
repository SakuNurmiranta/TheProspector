## Glossary

- **Actor:** Semi-autonomous character acting within a collective (e.g., band members). Actors derive from `GameEntity` and can perform character actions within the constraints of collective actions.

- **Agent:** Low-autonomy helper character specializing in one or two tasks. Agents act only when directed and have limited scope.

- **All Over the Place:** Gestation strain penalty; forces resolution or replacement of a transient tag before the end of the turn.

- **Anti-Tag:** The opposing semantic tag in a tag–anti-tag pair.

- **Aspect:** A trait capable of hosting tags and forming ideas (e.g., guitar, corpse paint, lyricism). Some aspects are initially considered illegitimate until proven otherwise through play.

- **Associated Tag:** A tag thematically linked to another tag, often appearing as a permutation or resonance.

- **Burnout:** Rehearsal strain penalty; the character temporarily loses access to their mood tag slot.

- **Bulletin Board:** A world object or interface element that can be targeted by promotional actions.

- **Character Action:** An individual action performed by an entity within the constraints of the chosen collective action.

- **Circle, The:** Refers both to the group of players who are not Pariahs and to the Keeper-controlled resources available to them. The Circle has significant influence over what is considered true.

- **Clandestine Actions:** Agent actions that increase notoriety for a player and/or the scene, often carrying risk.

- **Clout:** Credibility within the context of what is considered true. Maintaining true status generates clout; redefining true can generate significant clout at higher risk.

- **Collective:** A group of game entities acting together (e.g., a band, a recording).

- **Collective Action:** A unified action chosen for an entire collective (e.g., rehearse, gestate), which constrains available character actions.

- **Compose Action:** Begins or extends a track by placing ideas; establishes the root idea of a track.

- **Controller:** High-agency character defining collective actions (e.g., band leaders). Controllers are represented in play as controller-type Actors.

- **Conveyance:** A normalized floating-point value (0.0–1.0) describing how clearly an idea or track transmits intent to the audience.

- **Demo Tape / Demo(n) Tape:** A raw, self-recorded cassette release composed of tracks. The primary output of the core game loop.

- **De-sacralising Tag-Pair:** A tag combined with a NULL tag (death metal logic; future scope).

- **Elites:** Hardcore listeners forming the credibility core of the scene. Elites are hypersensitive to authenticity and react strongly against simulacra.

- **End-game Event:** A cataclysmic event that collapses the scene and ends the game.

- **Fading to Obscurity:** A game-over state where a player’s output becomes inaccessible or irrelevant to both elites and posers.

- **Game Entity:** Any object capable of holding tags (characters, buildings, environments, tracks).

- **Generation (of Copy):** The duplication depth of a tape; each generation reduces conveyance.

- **Gestation Actions:** Actions used to extract tags, create ideas, and exchange concepts.

- **Gravity:** The pull exerted by TRUE tags or tag-pairs, shaping the scene’s thematic centre.

- **Gravity Factor:** The magnitude of thematic pull exerted by a TRUE tag or tag-pair.

- **Groupie:** An attachment or stat-boost character linked to another character or tag. Groupies do not act independently.

- **Idea:** An instantiated aspect carrying one or more tags.

- **Influence:** An abstract measure of standing within the scene.

- **Influence Points:** Quantified influence accumulated through actions, tags, and tracks.

- **In-Season Tag:** A tag naturally available during a specific season (e.g., morbid in autumn).

- **Keeper:** The player currently leading the scene. The Keeper wields powerful scene-shaping actions but is restricted in producing new material.

- **Keeper Actions:** High-impact actions used by the Keeper to legitimize, suppress, or sabotage elements within the scene.

- **Media, The:** An external force that misinterprets the actions of the Circle and brings attention from Posers. Media attention increases notoriety and sales but undermines legitimacy.

- **Mood Tag:** A short-term emotional tag with limited uses per turn. Excessive use risks producing simulacra.

- **Novelty / Novelty Factor:** The influence bonus applied when an aspect or permutation is introduced for the first time into the scene.

- **Notoriety:** A trait that increases visibility and sales while attracting Media attention. Exists at personal, Circle, and scene levels.

- **Outside, The:** Everything external to the Circle. Without elements originating from within the Circle, the Outside cannot produce anything considered true.

- **Overreach:** Keeper strain penalty; blocks influence gain and Pull replenishment.

- **Pariah:** A player cast out by Keeper action or vote. Pariahs have full artistic freedom but no institutional backing.

- **Player-to-System Interface (PTSI):** A UI-layer abstraction translating raw system values into player-facing representations based on character understanding.

- **Posers:** The mainstream audience reflecting commercial appeal rather than legitimacy.

- **Promotion / Promotional Actions:** Actions used to spread awareness of recordings beyond Keeper control.

- **Pull:** An expendable resource used by the Keeper to pay for Keeper actions.

- **Pure:** Describes something with no outside influence. Purity is mechanically degradable.

- **Rehearsal Actions:** Actions that refine ideas and improve conveyance within the rehearsal space.

- **Rehearse Action:** Improves a character’s conveyance value on a track.

- **Re-sacralising Tag-Pair:** A core TRUE-defining combination of a tag and its anti-tag.

- **Resonance Tag:** A long-term identity tag with unlimited use.

- **Retailer:** A location or entity that sells recordings.

- **Rest Action:** Skips activity to remove Tired status and avoid strain penalties.

- **Root (Idea):** The first idea placed on a track, defining its initial structure.

- **Sacralising Tag-Pair:** A double-tag pair (doom metal logic; future scope).

- **Scene:** The Snordenmark black metal subculture and its fluctuating thematic centre.

- **Scene Splintering:** The formation of a sub-scene caused by proselytizing TRUE tag-pairs.

- **Season (Turn):** One playable time segment within a year.

- **Selling Out:** A game-over state where a player’s output gains traction only among Posers.

- **SEMM91:** Scandinavian Extreme Metal Manager ’91; the planned expanded single-player version.

- **Simulacra:** An emulated or hollow expression of a tag, typically caused by overuse of mood or conviction tags. Accepted by Posers, rejected by Elites.

- **Snordenmark (SNO):** Fictional Nordic amalgam nation serving as the primary setting.

- **Social Hub:** A location where band members socialize outside of music production.

- **Special Tag:** A rare or high-risk tag obtained from specific events.

- **Strain Penalty:** A negative effect triggered by overexertion, specific to the type of action taken.

- **Structure:** A non-actor `GameEntity` representing locations such as libraries, police stations, or churches.

- **Tag:** A core emotional or ideological semantic marker.

- **Tag-Pair:** A structured combination of tags, often required for legitimacy.

- **The 2:3 Rule:** An entity may take two actions freely and a third at a cost.

- **The Rule of 7:** A design constraint limiting sets to seven elements (e.g., seven tag–anti-tag pairs, seven band members).

- **Tired Status:** A condition acquired by taking a third action in a turn.

- **Too on the Nose:** Promotional strain penalty; forces negative interaction with a convictional anti-tag.

- **Transient Tag:** A temporary tag acquired from interactions; must be used or it fades.

- **Transient Tag Slot:** Storage slot for a single uncommitted transient tag.

- **TRUE (uppercase):** Content mechanically legitimized by a re-sacralising tag-pair.

- **true (lowercase):** Scene-conforming surface-level aesthetic or trend.

- **Wave, 1st:** Progenitors of what is considered TRUE; the reference point for early legitimacy.

- **Wave, 2nd:** The current output of the scene, struggling to remain TRUE and Pure.

- **Wave, 3rd:** A hypothetical illegitimate future output, often framed as a catastrophic failure of the scene.

- **Year (Round):** A full cycle of four seasons.
