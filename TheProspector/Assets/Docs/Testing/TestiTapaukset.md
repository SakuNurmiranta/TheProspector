## Projektin nimi: Demo(n)Tapes – Verkkopelidemo (SEMM91)

Alla 6 kpl testitapauksia, kirjoitettuna mallisi mukaan. Voit muokata ID:t, nimet, päivämäärät ja todelliset tulokset itse.

---

### Testitapaus 1 – Perus lockstep ja kierros/vuosilogiikka

Testitapauksen ID: **Fun_01**  
Testin suunnittelija: \<Nimi\>  
Testin prioriteetti (Low/Medium/High): **High**  
Testin suunnittelupäivä: \<Päivä\>

Moduulin nimi: **Kierros- ja vuosilogiikka (GameCoordinator)**  
Testaaja: \<Nimi\>  
Testin otsikko: **Kolmen pelaajan vuorojen lockstep ja vuoden vaihtuminen neljän globaalin vuoron jälkeen**  
Testin suorituspäivä: \<Päivä\>

Kuvaus:  
Testataan, että kolme pelaajaa jakavat saman globaalin vuoron ja vuoden, että kukin aktiivinen pelaaja voi tehdä vain yhden toiminnon per vuoro, ja että vuosi kasvaa aina neljän globaalin vuoron jälkeen kaikilla peliklienteillä.

Esivaatimukset:
- Unity-projekti on käynnissä host- ja kahdessa ParrelSync-clone-ikkunassa.
- NetBootstrap UI näkyvissä (LOCALHOST / LOCALCLIENT painikkeet).
- GameCoordinator-prefab on rekisteröity NetworkManageriin.

Riippuvuudet:
- Unity Netcode for GameObjects
- Unity Transport (UTP)
- ParrelSync-klonit toiminnassa

| Step | Testin vaiheet                                                                 | Testi Data                                                | Odotettu tulos                                                                                     | Todellinen tulos       | Tila (Läpi/Hylätty) | Notes |
|------|-------------------------------------------------------------------------------|-----------------------------------------------------------|----------------------------------------------------------------------------------------------------|------------------------|---------------------|-------|
| 1    | Käynnistä host-peli pääikkunassa                                             | Paina LOCALHOST                                           | Host-ikkunan scoreboard näyttää Year = 0, Turn = 0                                                | \<Täytetään testissä\> |                     |       |
| 2    | Käynnistä kaksi klienttiä kahdessa clone-ikkunassa                           | Paina LOCALCLIENT molemmissa                              | Kaikki kolme ikkunaa näyttävät samat Year- ja Turn-arvot (0,0)                                    |                        |                     |       |
| 3    | Paina SPACE host-ikkunassa (End Turn)                                        | -                                                         | Hostin Score kasvaa 1, Exhausted = true. Turn ei vielä vaihdu (muut eivät ole toimineet).          |                        |                     |       |
| 4    | Paina SPACE Client 1 -ikkunassa                                              | -                                                         | Client 1:n Score kasvaa 1, Exhausted = true. Turn ei vielä vaihdu.                                |                        |                     |       |
| 5    | Paina SPACE Client 2 -ikkunassa                                              | -                                                         | Client 2:n Score kasvaa 1, Exhausted = true. Nyt kaikki aktiiviset pelaajat ovat toimineet → Turn = 1 kaikilla. |                        |                     |       |
| 6    | Toista vaiheet 3–5 kunnes Turn-arvo on kasvanut neljä kertaa (0→3)           | -                                                         | Jokaisen kierroksen jälkeen Turn kasvaa yhdellä. Vuosi kasvaa yhdellä, kun Turn saavuttaa 4 ja resetoi (Year = 1, Turn = 4→0 tms. suunnitelman mukaan). |                        |                     |       |
| 7    | Yritä painaa SPACE kaksi kertaa saman vuoron aikana samalla pelaajalla       | -                                                         | Toisella painalluksella mitään ei tapahdu ennen kuin globaali Turn on vaihtunut seuraavaan arvoon. |                        |                     |       |

Jälkiehto / tilanne:
- Kaikilla klienteillä on identtinen Year- ja Turn-arvo jokaisessa vaiheessa.
- Yksikään pelaaja ei voi toimia kahta kertaa samalla vuorolla.
- Vuosi kasvaa täsmälleen neljän globaalin vuoron jälkeen.

---

### Testitapaus 2 – Keeperin valinta ja vaihtuminen vuoden lopussa

Testitapauksen ID: **Fun_02**  
Testin suunnittelija: \<Nimi\>  
Testin prioriteetti (Low/Medium/High): **High**  
Testin suunnittelupäivä: \<Päivä\>

Moduulin nimi: **Keeper-logiikka ja vuosiresoluutio**  
Testaaja: \<Nimi\>  
Testin otsikko: **Keeperin deterministinen valinta ja vaihtuminen, kun toinen pelaaja saa korkeamman pistemäärän vuoden lopussa**  
Testin suorituspäivä: \<Päivä\>

Kuvaus:  
Testataan, että Keeper valitaan deterministisesti (esim. pienin clientId) ja että vuoden lopussa Keeper vaihtuu pelaajalle, jolla on korkein Score, sekä että vaihdos näkyy kaikilla klienteillä.

Esivaatimukset:
- Testitapaus 1 (lockstep ja vuosi) on läpäisty.
- Kolme pelaajaa yhdistettynä samaan sessioon.

Riippuvuudet:
- `YearEndKeeperValidityCheck()` ja `UpdateLastResolvedRound()` toiminnassa.

| Step | Testin vaiheet                                                              | Testi Data                         | Odotettu tulos                                                                                         | Todellinen tulos       | Tila (Läpi/Hylätty) | Notes |
|------|----------------------------------------------------------------------------|------------------------------------|--------------------------------------------------------------------------------------------------------|------------------------|---------------------|-------|
| 1    | Käynnistä host ja 2 klienttiä kuten testissä Fun_01                        | -                                  | Scoreboard näyttää Keeperin clientId:n samalla tavalla kaikissa ikkunoissa.                           | \<Täytetään testissä\> |                     |       |
| 2    | Merkitse muistiin nykyinen Keeper (esim. Client 0)                         | -                                  | Kaikki ikkunat näyttävät saman Keeper-arvon.                                                           |                        |                     |       |
| 3    | Pelaa yksi vuosi (4 globaalia vuoroa) siten, että jokainen painaa SPACE kerran per vuoro | -                                  | Vuoden lopussa (Turn = 4*n) Score summautuu, Keeper voi pysyä samana vielä tässä vaiheessa.           |                        |                     |       |
| 4    | Seuraavan vuoden aikana anna yhdelle Regular-pelaajalle selvästi enemmän pisteitä (enemmän End Turn -toimintoja) | Extra SPACE-painalluksia yhdelle Regularille | Vuoden vaihtuessa tämän pelaajan Score on suurempi kuin Keeperin.                                     |                        |                     |       |
| 5    | Tarkista scoreboard kaikissa ikkunoissa heti vuoden vaihduttua             | -                                  | Keeper-kenttä päivittyy tälle eniten pisteitä saaneelle pelaajalle kaikissa klipeissä.                |                        |                     |       |
| 6    | Varmista, että `lastResolvedRound` (debugin tai logien kautta) vastaa vuoden lopun pistetilannetta | - | Snapshot sisältää jokaisen playerId:n Score- ja IsActive-arvot, jotka vastaavat scoreboardin tilannetta. |                        |                     |       |

Jälkiehto / tilanne:
- Keeper on vuoden lopussa aina korkein Score-arvoinen pelaaja.
- Kaikilla klienteillä on sama Keeper.
- Vuoden lopun snapshot on sama kaikkien pelaajien `lastResolvedRound`-sanakirjoissa.

---

### Testitapaus 3 – Pelaajatilojen (Score / Exhausted / Active) synkronointi

Testitapauksen ID: **Fun_03**  
Testin suunnittelija: \<Nimi\>  
Testin prioriteetti (Low/Medium/High): **High**  
Testin suunnittelupäivä: \<Päivä\>

Moduulin nimi: **Pelaajatilat (NetPlayerState)**  
Testaaja: \<Nimi\>  
Testin otsikko: **Score-, Exhausted- ja Active-arvojen synkronointi kaikkien klienteiden välillä**  
Testin suorituspäivä: \<Päivä\>

Kuvaus:  
Testataan, että End Turn ja Skip Turn päivittävät pelaajan Score- ja Exhausted-arvot oikein ja että nämä muutokset näkyvät yhdenmukaisesti kaikilla klienteillä. Lisäksi varmistetaan, että IsActive vaikuttaa siihen, voiko pelaaja toimia.

Esivaatimukset:
- Kolme pelaajaa yhdistetty samaan sessioon.
- Scoreboard-näkymä käytössä GameCoordinatorin kautta.

Riippuvuudet:
- `NetPlayerState` NetworkVariables toiminnassa.

| Step | Testin vaiheet                                                          | Testi Data | Odotettu tulos                                                                                               | Todellinen tulos       | Tila (Läpi/Hylätty) | Notes |
|------|-------------------------------------------------------------------------|-----------|--------------------------------------------------------------------------------------------------------------|------------------------|---------------------|-------|
| 1    | Host painaa SPACE kerran                                                | -         | Hostin rivillä Score = 1, Exhausted = true. Sama näkyy molemmissa klientti-ikkunoissa.                      | \<Täytetään testissä\> |                     |       |
| 2    | Host painaa BACKSPACE seuraavalla vuorolla                              | -         | Hostin Score pysyy samana, Exhausted muuttuu false:ksi kaikissa ikkunoissa.                                 |                        |                     |       |
| 3    | Hostin IsActive asetetaan falseksi (esim. keinotekoisesti debugilla tai disconnectilla) | -         | Hostin Active-arvo false (tai rivi poistuu, jos pelaaja despawnataan), eikä se enää voi vaikuttaa vuoron etenemiseen. |                        |                     |       |
| 4    | Vain Client 1 ja Client 2 painavat SPACE, kun Host on inactive          | -         | Turn etenee, vaikka Host ei tee mitään; scoreboardin Active-arvo ohjaa lockstep-käyttäytymistä.             |                        |                     |       |

Jälkiehto / tilanne:
- Score, Exhausted ja Active ovat yhdenmukaiset kaikilla klienteillä jokaisen muutoksen jälkeen.
- Vain aktiiviset pelaajat huomioidaan lockstep-logiikassa.

---

### Testitapaus 4 – Keeperin disconnect (ei-host) ja uuden Keeperin valinta

Testitapauksen ID: **Fun_04**  
Testin suunnittelija: \<Nimi\>  
Testin prioriteetti (Low/Medium/High): **High**  
Testin suunnittelupäivä: \<Päivä\>

Moduulin nimi: **Keeper disconnect -logiikka**  
Testaaja: \<Nimi\>  
Testin otsikko: **Keeper-pelaajan (ei-host) disconnect ja uuden Keeperin deterministinen valinta**  
Testin suorituspäivä: \<Päivä\>

Kuvaus:  
Testataan tilanne, jossa Keeper on yksi klienteistä (ei host) ja kyseinen pelaaja poistuu pelistä (ikkuna suljetaan). Varmistetaan, että peli jatkuu, uusi Keeper valitaan deterministisesti ja tämä näkyy kaikilla klienteillä.

Esivaatimukset:
- Kolme pelaajaa, Keeperiksi valittu joku muista kuin hostista (esim. pistelogiin avulla).

Riippuvuudet:
- `OnClientDisconnected` ja `ElectKeeperFromLastResolvedRound` / `EnsureKeeperSelected` toiminnassa.

| Step | Testin vaiheet                                                         | Testi Data | Odotettu tulos                                                                                       | Todellinen tulos       | Tila (Läpi/Hylätty) | Notes |
|------|------------------------------------------------------------------------|-----------|------------------------------------------------------------------------------------------------------|------------------------|---------------------|-------|
| 1    | Varmista, että Keeper on jokin klientti (esim. Client 1)              | -         | Scoreboard kaikissa ikkunoissa näyttää Keeper = Client 1.                                            | \<Täytetään testissä\> |                     |       |
| 2    | Sulje Client 1 -ikkuna kokonaan                                       | -         | Client 1 poistuu pelistä; host ja Client 2 jäävät.                                                  |                        |                     |       |
| 3    | Tarkista hostin ja Client 2:n scoreboardin Keeper-kenttä              | -         | Uusi Keeper on valittu (esim. pienin jäljellä oleva clientId).                                      |                        |                     |       |
| 4    | Pelaa muutama vuoro hostin ja jäljellä olevan klientin kanssa         | -         | Peli jatkuu normaalisti: Turn ja Year etenevät eikä minkään ikkunan UI jää jumiin.                  |                        |                     |       |

Jälkiehto / tilanne:
- Peli on edelleen pelattavissa kahdella pelaajalla.
- Uusi Keeper on sama kaikilla klienteillä.
- Disconnect ei aiheuta kaatumista tai deadlockia.

---

### Testitapaus 5 – Tavallisen pelaajan disconnect

Testitapauksen ID: **Fun_05**  
Testin suunnittelija: \<Nimi\>  
Testin prioriteetti (Low/Medium/High): **Medium**  
Testin suunnittelupäivä: \<Päivä\>

Moduulin nimi: **Regular-pelaajan disconnect**  
Testaaja: \<Nimi\>  
Testin otsikko: **Ei-Keeper-pelaajan poistuminen pelistä kesken vuoden**  
Testin suorituspäivä: \<Päivä\>

Kuvaus:  
Testataan tilanne, jossa tavallinen (Regular) pelaaja poistuu pelistä kesken vuoden. Varmistetaan, että peli jatkuu kahden pelaajan välillä ja lockstep ottaa huomioon pienemmän pelaajamäärän.

Esivaatimukset:
- Keeper on host tai joku muu kuin poistettava pelaaja.
- Kolme pelaajaa yhdistettynä.

Riippuvuudet:
- Sama disconnect-käsittely kuin testissä Fun_04.

| Step | Testin vaiheet                                                    | Testi Data | Odotettu tulos                                                                                   | Todellinen tulos       | Tila (Läpi/Hylätty) | Notes |
|------|-------------------------------------------------------------------|-----------|--------------------------------------------------------------------------------------------------|------------------------|---------------------|-------|
| 1    | Varmista, että Keeper on esimerkiksi host                         | -         | Scoreboard näyttää Keeper = hostin clientId.                                                    | \<Täytetään testissä\> |                     |       |
| 2    | Sulje yhden Regular-pelaajan (esim. Client 2) ikkuna             | -         | Client 2 poistuu scoreboardilta; jäljellä on 2 pelaajaa.                                        |                        |                     |       |
| 3    | Jatka pelaamista hostilla ja jäljellä olevalla klientillä        | -         | Peli etenee normaalisti, Turn ja Year päivittyvät, eikä game flow jää odottamaan puuttuvaa pelaajaa. |                        |                     |       |

Jälkiehto / tilanne:
- Peli toimii edelleen kahden pelaajan välillä ilman virheitä.
- Disconnect ei riko lockstep- tai Keeper-laskentaa.

---

### Testitapaus 6 – Mid-session join (myöhäinen liittyminen)

Testitapauksen ID: **Fun_06**  
Testin suunnittelija: \<Nimi\>  
Testin prioriteetti (Low/Medium/High): **Medium**  
Testin suunnittelupäivä: \<Päivä\>

Moduulin nimi: **Mid-session join -logiikka**  
Testaaja: \<Nimi\>  
Testin otsikko: **Uuden pelaajan liittyminen kesken pelin ja aktivointi seuraavan vuoden alussa**  
Testin suorituspäivä: \<Päivä\>

Kuvaus:  
Testataan, että uusi pelaaja voi liittyä käynnissä olevaan sessioon näkemättä väärää turn/vuosi-tilaa, että hän spawnaa aluksi inactive-tilassa, ja että hän aktivoituu automaattisesti seuraavan vuoden alussa.

Esivaatimukset:
- Host ja yksi klientti ovat pelanneet jo vähintään yhden vuoden (esim. Year ≥ 1, Turn > 0).

Riippuvuudet:
- `TryStartWhenThree()` käyttää `_gameStarted`-lippua, jotta peli ei resetoi mid-joinin aikana.
- `ReactivateInactivePlayersAtYearEnd()` käytössä.

| Step | Testin vaiheet                                                             | Testi Data | Odotettu tulos                                                                                                      | Todellinen tulos       | Tila (Läpi/Hylätty) | Notes |
|------|----------------------------------------------------------------------------|-----------|---------------------------------------------------------------------------------------------------------------------|------------------------|---------------------|-------|
| 1    | Pelaa hostilla ja Client 1:llä niin, että Year ≥ 1 ja Turn > 0            | -         | Scoreboard näyttää saman Year- ja Turn-arvon hostilla ja Client 1:llä.                                            | \<Täytetään testissä\> |                     |       |
| 2    | Avaa uusi clone-ikkuna ja paina LOCALCLIENT                               | -         | Uusi klientti liittyy peliin. Kaikissa ikkunoissa Year ja Turn pysyvät samoina (ei resetointia).                   |                        |                     |       |
| 3    | Tarkista uuden pelaajan rivi scoreboardilla                               | -         | Score = 0, Exhausted = false, Active = false. Keeper on sama kaikissa ikkunoissa.                                  |                        |                     |       |
| 4    | Jatka peliä kunnes seuraava vuoden vaihto (4 globaalia vuoroa lisää)      | -         | Vuoden vaihtuessa `ReactivateInactivePlayersAtYearEnd` asettaa uuden pelaajan Active = true kaikilla klienteillä.  |                        |                     |       |
| 5    | Tarkista, että uusi pelaaja nyt osallistuu lockstepiin                    | -         | Turn ei etene, ellei myös uusi pelaaja tee End/Skip Turn -toimintaa (kuten muut aktiiviset pelaajat).             |                        |                     |       |

Jälkiehto / tilanne:
- Uusi pelaaja liittyy kesken pelin ilman, että olemassa oleva pelitila resetoituu.
- Uusi pelaaja aktivoituu automaattisesti seuraavan vuoden alussa ja osallistuu lockstep-kierroksiin normaalisti.

---
