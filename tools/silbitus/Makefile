syls.txt: create_syls_file silbitus.dic
	./create_syls_file

create_syls_file: create_syls.c stb_ds.h
	gcc -o create_syls_file create_syls.c -Wall -Wextra -Werror

stb_ds.h:
	wget "https://raw.githubusercontent.com/nothings/stb/refs/heads/master/stb_ds.h"

silbitus.dic: licence
	wget "https://arhiiv.eki.ee/tarkvara/wordlist/silbitus.dic"

licence:
	wget "https://arhiiv.eki.ee/eki/litsents.html"

clean:
	-rm silbitus.dic*
	-rm stb_ds.h*
	-rm create_syls_file*

