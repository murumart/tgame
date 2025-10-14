
### Üldine

Segu strateegiamängust ning linnaehitusmängust.

Protseduuriliselt genereeritud maailmakaart on jagatud maakondadeks, millel igal võib olla omanik mingi suure entiteedi, nt riigi näol, ning millel asetseb ka kohalik asustus. Mängija ülesanne on ühte sellist asulat hallata, et riigile / muule isandale kasulikke asju toota ning saata.

Mängijale nähtav kaart on ruudustik. Mõõtkava on selline, et üks ruut mahutab ära ühe suure ehitise või ühe metsatuka. Üksikud inimesed pole hallatavad, on ära abstraheeritud. 

### Mootori kasutamine

Hoidkem C# abil mälus mänguobjekte, ning Godot's stseeni avamisel laadigem see visuaalselt ning mängijale "katsutavalt" - ehk mänguloogika ning -graafika hoida eraldi.

### Visuaalid

Värvipalett olgu "[31](https://lospec.com/palette-list/31)".

### Arendusplaan

#### Oktoober 2025

Eesmärk jõuda prototüübiga võrreldavale tasemele. *Game Loop*. Mängus on olemas:

- [ ] aeg jookseb
	- [ ] saab pausile panna
	- [ ] ehitiste ehitamine võtab aega
- [ ] ruutude ui
- [ ] ui ehitistele vajutades
	- [ ] ehitise nimi, inimesed sees, tegevused ehitisega
- [ ] tegevused ehitisega
	- [ ] inimeste palkamine ehitamiseks
	- [ ] inimeste palkamine tööle
- [ ] ressursid
	- [ ] ehitiste ehitamine võtab ressursse
- [ ] rahvas
	- [ ] populatsiooniarv UIs
	- [ ] eraldi arvud majades inimeste ning kodutute kohta
	- [ ] eraldi arvud töös inimeste ning töötute kohta
- [ ] tootlus
- [ ] tootluskvoot
	- [ ] lihtne gameover: kvoodi täitmata jätmine
