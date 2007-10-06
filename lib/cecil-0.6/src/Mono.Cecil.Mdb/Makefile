thisdir = class/Mono.Cecil.Mdb
include ../../build/rules.make

LIBRARY = Mono.Cecil.Mdb.dll
LIBRARY_SNK = ../mono.snk
LIBRARY_PACKAGE = none

LIB_MCS_FLAGS = /r:$(corlib) /r:Mono.Cecil.dll /d:CECIL -keyfile:$(LIBRARY_SNK)

NO_TEST = yes

include ../../build/library.make
