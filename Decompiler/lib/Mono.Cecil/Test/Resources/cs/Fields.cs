using System;
using System.Runtime.InteropServices;

class Foo {
	Bar bar;
}

class Bar {
	volatile int oiseau;
}

class Baz {
	bool @bool;
	char @char;
	sbyte @sbyte;
	byte @byte;
	short int16;
	ushort uint16;
	int int32;
	uint uint32;
	long int64;
	ulong uint64;
	float single;
	double @double;
	string @string;
	object @object;
}

enum Pim {
	Pam = 1,
	Poum = 2,
}

class PanPan {

	public const PanPan Peter = null;
	public const string QQ = "qq";
	public const string nil = null;
	public const object obj = null;
	public const int [] ints = null;
}
