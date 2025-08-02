Aja möödumine mängus. Kuidas?

**Kogu aeg aeg möödub**
Iga maailmakaardiruutu simuleeritakse pidevalt. Kui on suurem kaardi osa lahti, siis aeglane?

**Simuleeri kaardiruudu avamise hetkel kogu möödunud aega**
Aeglane ajajooksu hetkel. Teistes ruutudes [[asjade]] tootmine ei töötaks siis, ei saaks toodetud ressursse mujal kasutada...

Kuigi kogu kaart võiks siiski mälus olla (c# struktuuridena eelkõige?), siis oleks esimene variant parem

**Ajahetke möödumine**
Ikkagi n-ö process-funktsiooni järgi eri objektidel aeg möödub. Aga nt kui objekt peab iga päeva möödudes mingit asja tegema, ning möödub korraga kahe päeva aeg, siis võib see päeva möödumise loogika vaid ühe korra joosta. Aja möödumine peaks olema siis minuti täpsusega tsüklis tehtud?

Kas oleks eelnev võimalik asendada eventidega? Iga ajaühiku möödumisel eraldi event. Asendada ei saa, siis peab ju eventide emiteerimisel vaatama, et "päev möödus" event antakse välja nii mitu korda, kui päev möödus. Kas ajaliselt tekib seal probleem?