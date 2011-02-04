using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: Foo ("bingo")]

[assembly: TypeForwardedTo (typeof (System.Diagnostics.DebuggableAttribute))]

enum Bingo : short {
	Fuel = 2,
	Binga = 4,
}

/*
in System.Security.AccessControl

	[Flags]
	public enum AceFlags : byte {
		None = 0,
		ObjectInherit = 0x01,
		ContainerInherit = 0x02,
		NoPropagateInherit = 0x04,
		InheritOnly = 0x08,
		InheritanceFlags = ObjectInherit | ContainerInherit | NoPropagateInherit | InheritOnly,
		Inherited = 0x10,
		SuccessfulAccess = 0x40,
		FailedAccess = 0x80,
		AuditFlags = SuccessfulAccess | FailedAccess,
	}
*/

class FooAttribute : Attribute {

	internal class Token {
	}

	public FooAttribute ()
	{
	}

	public FooAttribute (string str)
	{
	}

	public FooAttribute (sbyte a, byte b, bool c, bool d, ushort e, short f, char g)
	{
	}

	public FooAttribute (int a, uint b, float c, long d, ulong e, double f)
	{
	}

	public FooAttribute (char [] chars)
	{
	}

	public FooAttribute (object a, object b)
	{
	}

	public FooAttribute (Bingo bingo)
	{
	}

	public FooAttribute (System.Security.AccessControl.AceFlags flags)
	{
	}

	public FooAttribute (Type type)
	{
	}

	public int Bang { get { return 0; } set {} }
	public string Fiou { get { return "fiou"; } set {} }

	public object Pan;
	public string [] PanPan;

	public Type Chose;
}

[Foo ("bar")]
class Hamster {
}

[Foo ((string) null)]
class Dentist {
}

[Foo (-12, 242, true, false, 4242, -1983, 'c')]
class Steven {
}

[Foo (-100000, 200000, 12.12f, long.MaxValue, ulong.MaxValue, 64.646464)]
class Seagull {
}

[Foo (new char [] { 'c', 'e', 'c', 'i', 'l' })]
class Rifle {
}

[Foo ("2", 2)]
class Worm {
}

[Foo (new object [] { "2", 2, 'c' }, new object [] { new object [] { 1, 2, 3}, null })]
class Sheep {
}

[Foo (Bang = 42, PanPan = new string [] { "yo", "yo" }, Pan = new object [] { 1, "2", '3' }, Fiou = null)]
class Angola {
}

[Foo (Pan = "fiouuu")]
class BoxedStringField {
}

[Foo (Bingo.Fuel)]
class Zero {
}

[Foo (System.Security.AccessControl.AceFlags.NoPropagateInherit)]
class Ace {
}

[Foo (new object [] { Bingo.Fuel, Bingo.Binga }, null, Pan = System.Security.AccessControl.AceFlags.NoPropagateInherit)]
class Bzzz {
}

[Foo (typeof (Bingo))]
class Typed {
}

[Foo (typeof (FooAttribute.Token))]
class NestedTyped {
}

[Foo (Chose = typeof (Typed))]
class Truc {
}

[Foo (Chose = (Type) null)]
class Machin {
}

[Foo (typeof (Dictionary<,>))]
class OpenGeneric<X, Y> {
}

[Foo (typeof (Dictionary<string, OpenGeneric<Machin, int>[,]>))]
class ClosedGeneric {
}
