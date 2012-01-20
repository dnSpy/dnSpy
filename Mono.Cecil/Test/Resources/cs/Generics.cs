using System;
using System.Collections.Generic;

class Foo<TBar, TBaz> {}

abstract class Bar<T> {

	T bang;

	public abstract Bar<T> Self ();

	public abstract Bar<string> SelfString ();
}

abstract class Baz {

	public abstract TBang Gazonk<TBang> (object o);

	public abstract Bar<TBingo> Gazoo<TBingo> ();
}

class Zap {}
interface IZoom {}

class Bongo<T> where T : Zap, IZoom {

	enum Dang {
		Ding = 2,
		Dong = 12,
	}
}

class Parent<T> {}
class Child<T> : Parent<T> {
	public T [] array;
}
class TamChild : Child<Tamtam> {}
class RecChild : Child<RecChild> {}

class Tamtam {

	static void Foo<TFoo> (TFoo tf)
	{
	}

	static void Bar ()
	{
		Foo (2);
	}

	static List<TBeta> Beta<TBeta> ()
	{
		return new List<TBeta> ();
	}

	static List<TCharlie> Charlie<TCharlie> ()
	{
		return new List<TCharlie> ();
	}
}

class It {

	public IEnumerable<Foo<string, string>> Pwow ()
	{
		yield return new Foo<string, string> ();
		yield return new Foo<string, string> ();
		yield return new Foo<string, string> ();
	}

	public void ReadPwow ()
	{
		foreach (Foo<string, string> foo in Pwow ())
			Tac (foo);
	}

	public void Tac<T> (T t)
	{
	}
}

class Duel<T1, T2, T3> where T2 : T1 where T3 : T2 {}

class ChildReader {

	public int Read (TamChild t)
	{
		return t.array.Length;
	}
}

struct Nilible<T> where T : struct {
	public T t;
}

class Null {

	public static int Compare<T> (Nilible<T> x, Nilible<T> y) where T : struct
	{
		return Comparer<T>.Default.Compare (x.t, y.t);
	}
}

public class DoubleFuncClass {
	public void Test () { Test<int> (); Test<int, int> (); }
	public void Test<T> () { Test<T, int> (); Test<T, T> (); }
	public void Test<T, T1> () { Test<T1> (); Test<T1, T> (); }
}

public class DoubleFuncClass<X> {
	public void Test () { Test<int> (); Test<int, int> (); }
	public void Test<T> () { Test<X, int> (); Test<T, X> (); }
	public void Test<T, T1> () { Test<T1> (); Test<T1, T> (); }
}

public class LaMatrix {
	public static T At<T> (T[,] m, int i, int j)
	{
		return m [i, j];
	}
}
