using System;
using System.Runtime.InteropServices;

abstract class Foo {
	public abstract void Bar (int a);
}

class Bar {

	[DllImport ("foo.dll")]
	public extern static void Pan ([MarshalAs (UnmanagedType.I4)] int i);
}

public class Baz {

	public void PrintAnswer ()
	{
		Console.WriteLine ("answer: {0}", 42);
	}
}

