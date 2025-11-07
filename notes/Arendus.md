## Kuude kaupa

### Oktoober 2025

Eesmärk jõuda prototüübiga võrreldavale tasemele. *Game Loop*. Mängus on olemas:

- [X] aeg jookseb
	- [X] saab pausile panna
	- [X] ehitiste ehitamine võtab aega
	- [ ] fookuses regiooni aeg peaks detailsemalt jooksma
- [ ] ruutude ui
	- [X] ui ehitise peale hiirega minnes
		- [ ] ehitise nimi, inimesed sees, tegevused ehitisega
- [X] tegevused ehitisega
	- [X] inimeste palkamine ehitamiseks
	- [X] inimeste palkamine tööle
- [ ] ressursimaardlad
	- [ ] andmeesitus
		- [ ] defineerivad tööd, mida nende juures teha saab
	- [ ] visuaalesitus
- [X] ressursid
	- [X] ehitiste ehitamine võtab ressursse
- [X] rahvas
	- [X] populatsiooniarv UIs
	- [X] eraldi arvud majades inimeste ning kodutute kohta
	- [X] eraldi arvud töös inimeste ning töötute kohta
- [ ] tootlus
- [ ] tootluskvoot
	- [ ] lihtne gameover: kvoodi täitmata jätmine

### November 2025

Eesmärk jõuda oktoobris plaanitud tasemele(!!). Alustada tööd esimese verstaposti jaoks.

Kriitiline: inimeste määramine tööle, tootluskvoodid emamaalt -- ilma nendeta pole mängu!!

Nõu pidada UI disaini teemal (nüri ning aeganõudev).

## Verstapostid

Mäng peab olema mängitav iga verstaposti saabudes.

### 0.1.0

- [ ] genereeritud maailm
	- [ ] müra: kõrgus, sügavus, et oleks vahe mere ja maa vahel
	- [ ] müra: jõed (hallid alad)
	- [ ] regioonideks jaotamine (voronoi? mingisugune juhuslik "paint bucket"?)
	- [ ] eri pooled
		- [ ] AI käitugu praegu täiesti suvaliselt kus seda on lihtne implementeerida
- [ ] ruudud millega mängida
	- [ ] metsad puuraidumiseks
	- [ ] kivid kiviraidumiseks
	- [ ] vesi kala püüdmiseks
	- [ ] turg äri ajamiseks (suurem teema kui eelnevad!)
- [ ] mängumehaanikad
	- [ ] populatsioon sööb toitu, kasvab sündidest, kahaneb surmadest
	- [ ] emamaa peab arvet ressurssidest mida kasutab, saadab nende ning regiooni asukoha põhjal mandaate
	- [ ] populatsioonist tööliste määramine
- [ ] kettalt salvestamine ja laadimine (suur teema)

### 0.2.0

- [ ] AI pooled
	- [ ] implementeerida "utility theory" sisendid ja väljundi valimine
	- [ ] meetod kuidas AI tegevusi vaadelda, et mängija saaks aru, et mäng sellega tegeleb

### 0.3.0

- [ ] võitlused poolte vahel
	- [ ] AI saadetud rünnakud
	- [ ] mängija saadetud rünnakud
- [ ] genereeritud maailm: müra: temperatuur ja niiskus et oleks alus eri bioomideks

### Määramata tulevik

- populatsiooni peab palkama, et nad tööd teeks
- populatsiooni heaolu loeb - inimesed lahkuvad kehvast kolooniast