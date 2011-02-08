using System;

delegate void Pan (object sender, EventArgs args);

abstract class Foo {

	public abstract event Pan Bar;
}
