WGET := wget
CC := gcc

syls.txt: create_syls_file silbitus.dic
	./create_syls_file

create_syls_file: create_syls.c stb_ds.h
	$(CC) -o create_syls_file create_syls.c -Wall -Wextra -Werror

stb_ds.h:
	$(WGET) "https://raw.githubusercontent.com/nothings/stb/refs/heads/master/stb_ds.h"

silbitus.dic: licence
	$(WGET) "https://arhiiv.eki.ee/tarkvara/wordlist/silbitus.dic"
	iconv -f iso-8859-1 -t utf-8 silbitus.dic > s.dic
	rm silbitus.dic
	mv s.dic silbitus.dic

licence:
	$(WGET) "https://arhiiv.eki.ee/eki/litsents.html"

clean:
	-rm silbitus.dic*
	-rm stb_ds.h*
	-rm create_syls_file*
	-rm litsents.html.*

