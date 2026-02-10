
# <MÄNGU NIMI>

Linnaehitusmäng, mille kulgu juhib arvutist jutuvestja.

## "Disainisambad"

### 1. Metoodiline

**Absoluutselt rohkem mõistatusmäng kui märulimäng.**

Mängija peab saama läbi viia mõttekaid käike ning neid rahulikult kaaluda.

Mäng jookseb reaalajas, aga oma järgmiste käikude kavandamiseks saab mängu pausile panna. Juhtuvad asjad, mis mängijat sunnivad oma plaani muutma, kuid on ka hetked rahunemiseks ning enda loodud masinavärgi vaatlemiseks.

### 2.

**Pinge**

### 3.



## Mängumehaanikad

### Kolooniaehitus ning -haldus

Mängija planeerib ehitisi, milledesse palkab tööle inimesi. Inimesed toodavad esemeid vastavalt oma tööle ning töökohale, mis asub ehitises ("pagarikoda") või mõnes kohas mängualal ("karjäär").

## Omadused

### Maailmakaart

Protseduuriliselt genereeritud maailmakaart on jagatud maakondadeks, millel igal on omanik mingi "poole (faction)" näol, mis kujutab ka kohalikku asustust. Mängija ülesanne on ühte sellist asulat hallata, et emamaale kasulikke asju toota ning saata.

### Mängija vaade

Mängijale nähtav kaart on ruudustik. Mõõtkava on selline, et üks ruut mahutab ära ühe suure ehitise või ühe metsatuka. Üksikud inimesed pole hallatavad, on ära abstraheeritud. Kaardi peal ongi mängija ainuke liigutus ruutude peale vajutamine.

### Tööd

Süsteem asjade korraldamiseks enda maatükil. Tööle palgatakse inimesed ning nende hulgast ja palgast sõltub töö efektiivsus.

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
