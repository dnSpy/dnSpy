using System;
using System.Runtime.InteropServices;

[StructLayout (LayoutKind.Explicit, Size = 16)]
public struct Foo {
	[FieldOffset (0)] public ushort Bar;
	[FieldOffset (2)] public ushort Baz;
	[FieldOffset (4)] public uint Gazonk;
}

class Babar {
}

class Locke {
	public int [] integers = new int [] { 1, 2, 3, 4 };
}
