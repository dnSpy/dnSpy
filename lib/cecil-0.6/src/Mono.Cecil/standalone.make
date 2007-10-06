include ../../build/config.make

MCS = mcs
KEY_FILE = ../mono.snk
MCS_FLAGS = -keyfile:$(KEY_FILE)

all: Mono.Cecil.dll mono-cecil.pc

Mono.Cecil.dll: Mono.Cecil.dll.sources */*.cs
	$(MCS) $(MCS_FLAGS) @Mono.Cecil.dll.sources /target:library /out:Mono.Cecil.dll

clean:
	rm -f Mono.Cecil.dll
	rm -f mono-cecil.pc

mono-cecil.pc: mono-cecil.pc.in
	sed -e 's,@prefix@,$(prefix),g' mono-cecil.pc.in > $@.tmp
	mv $@.tmp $@

install: all mono-cecil.pc
	mkdir -p $(prefix)/lib/Mono.Cecil
	cp Mono.Cecil.dll $(prefix)/lib/Mono.Cecil
	cp mono-cecil.pc $(prefix)/lib/pkgconfig/mono-cecil.pc
