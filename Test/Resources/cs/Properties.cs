using System;

abstract class Foo {

	public abstract int Bar { get; }
	public abstract string Baz { get; set; }
	public abstract string Gazonk { set; }
}

abstract class Bar {

	public abstract Foo this [int a, string s] { set; }
}

class Baz {

	public string Bingo { get; set; }
}
