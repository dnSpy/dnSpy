using System;

interface IFoo {}
interface IBar : IFoo  {}

abstract class Bar : IBar {}

interface IBingo {
	void Foo ();
	void Bar ();
}

class Bingo : IBingo {

	void IBingo.Foo ()
	{
	}

	void IBingo.Bar ()
	{
	}
}
