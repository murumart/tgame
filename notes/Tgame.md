
# <MÄNGU NIMI>

Linnaehitusmäng, mille taustal toimub strateegiamäng.

## "Disainisambad"

### 1. Metoodiline

**Absoluutselt rohkem mõistatusmäng kui märulimäng.**

Mängija peab saama läbi viia mõttekaid käike ning neid rahulikult kaaluda.

Mäng jookseb reaalajas, aga oma järgmiste käikude kavandamiseks saab mängu pausile panna. Juhtuvad asjad, mis mängijat sunnivad oma plaani muutma, kuid on ka hetked rahunemiseks ning enda loodud masinavärgi vaatlemiseks.

### 2. Elav maailm

**Mängija mängib oma mängu, ülejäänud maailm mängib enda oma. See peab olema arusaadav ning arvestatav.**

Mängija peab aru saama, et ta pole maailmas ainuke "mõistuslik" olend.

Niisamuti kui mängija loodab tulu teenida, loodavad ka seda kõik teised osapooled maailmas. Nende käitumine mõjutab mängijat olulisel määral.

### 3. Mõjukas mängija

**Mängija peab saama oma tegevusega maailma mõjutada ka väljaspool mänguala. Kohalik tootlus mõjutab turgu, sidemeid, sõdu.**

Mängija peab tundma ennast maailma osana ning võimelisena seda muutma.

Näiteks turule suure hulga odava toidu paiskamine võiks loogiliselt vähendada teiste toidutootjate tulu ning suunata toiduostjaid endi eelarveid ümber hindama. Kui nüüd see kraan kinni keerata, satuks mõni eelmine ostja hätta, mõni tuleks ähvardustega mängija jutule.

## Mängumehaanikad

### Kolooniaehitus ning -haldus

Mängija planeerib ehitisi, milledesse palkab tööle inimesi. Inimesed toodavad esemeid vastavalt oma tööle ning töökohale, mis asub ehitises ("pagarikoda") või mõnes kohas mängualal ("karjäär").

### Diplomaatia

Kolooniana peab suhtlema emamaaga, kes sätib kvoodi mingi asja tootmiseks ning saadab selle täitmisel esemete vastu raha ning suuremad ootused. Kaubandus saab toimuda ka teiste ümbritsevate riikidega.

Emamaaga oleks võimalik rääkida enda autonoomia teemal ning järjest rohkem seda saada, kuni saavutatakse iseseisvus. Emamaa ründaks siis oma vägedega mängija regiooni.

### Automaat-RTS

Ümbritsevad riigid haldavad samuti kolooniaid/linnu. Samad dünaamikad, mis mängija linna ning emamaa vahel. Riigid kauplevad, kemplevad, sõdivad vastavalt arvutustele.

## Omadused

### Maailmakaart

Protseduuriliselt genereeritud maailmakaart on jagatud maakondadeks, millel igal võib olla omanik riigi näol, ning millel asetseb ka kohalik asustus. Mängija ülesanne on ühte sellist asulat hallata, et emamaale kasulikke asju toota ning saata.

### Mängija vaade

Mängijale nähtav kaart on ruudustik. Mõõtkava on selline, et üks ruut mahutab ära ühe suure ehitise või ühe metsatuka. Üksikud inimesed pole hallatavad, on ära abstraheeritud. Kaardi peal ongi mängija ainuke liigutus ruutude peale vajutamine.

### Tööd

Lai süsteem asjade korraldamiseks enda maatükil. Tööle palgatakse inimesed ning nende hulgast ja palgast sõltub töö efektiivsus.

Maja ehitamine - kui kiiresti maja ehitatakse. Pagarikojas küpsetamine - kui palju leiba toodetakse. Metsas puuraidumine - kui palju puitu kätte saadakse ja kui kiiresti.

### Populatsioon

Mängija käsutuses on arv inimesi. Just arv, sest ei simuleerita üksikisikuid. Siiski võetakse arvesse hulka statistikaid, nagu
- mis oskused inimestel on,
- kui terved nad on,
- ligipääs toidule, veele ning sanitaarteenustele,
- rahulolu eluoluga.

## Mootori kasutamine

Hoidkem C# abil mälus mänguobjekte, ning Godot's stseeni avamisel laadigem see visuaalselt ning mängijale "katsutavalt" - ehk mänguloogika ning -graafika hoida eraldi.

## Visuaalid

Inspireeritud "vanadest" isomeetrilistest mängudest, näiteks [OpenTTD](https://www.openttd.org/). Mängijale on põhiline visuaal someetriliselt joonistatud madala resolutsiooniga ruudukaart. Pikslikunst.

Värvipalett olgu "[31](https://lospec.com/palette-list/31)".

Kasutajaliides on kõrgema resolutsiooniga, tekst seriifideta. Lihtsad värvid, sest nendega on lihtsam kui tekstuuridega vm.
