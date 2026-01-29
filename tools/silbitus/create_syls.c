#include <stdio.h>

#include <stdlib.h>
#include <string.h>
#include <errno.h>

#define STB_DS_IMPLEMENTATION
#include "stb_ds.h"

#define INFILE "silbitus.dic"
#define OUTFILE "syls.txt"

typedef struct {
	char *key;
	float value;
} SyllableCount;

int compareKs(const void *a, const void *b) {
	const SyllableCount *ca = a;
	const SyllableCount *cb = b;
	return (int)((long)cb->value - (long)ca->value);
}

int main(void) {

	puts("Will write syllable counts to "OUTFILE);

	FILE *in = fopen(INFILE, "r");
	if (in == NULL) {
		fprintf(stderr, "Error opening file: %s\n", strerror(errno));
		return 1;
	}

	SyllableCount *syllables = NULL;
	hmdefault(syllables, 0.0f);
	sh_new_arena(syllables);

	size_t foundsyllables = 0;
	char line[50];
	while (fgets(line, 50, in)) {

		size_t sylstart = 0;
		size_t i = 0;
		while (line[i] != '\0') {
			char c = line[i];
			// end of syllable
			if (c == '-' || c == '?' || c == '+') {
				char syllable[10] = {0};
				memcpy(
					syllable,
					&line[sylstart],
					i - sylstart);
				float old = shget(syllables, syllable);
				shput(syllables, syllable, old + 1);
				sylstart = i + 1;
				foundsyllables += 1;
			}
			i++;
		}
		
	}

	fclose(in);

	qsort(
		syllables,
		hmlenu(syllables),
		sizeof(*syllables),
		compareKs
	);

	FILE *out = fopen(OUTFILE, "w");

	size_t syllablecount = hmlenu(syllables);
	for (size_t i = 0; i < syllablecount; i++) {
		SyllableCount *syl = &syllables[i];
		float val = (float)syl->value / (float)foundsyllables;
		if (val < 0.0005f) break; // enough
		fprintf(
			out,
			"%s %lf\n",
			syl->key,
			val
		);
	}

	fclose(out);

	puts("Done writing syllable counts.");

	return 0;
}
