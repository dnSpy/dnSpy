using System.Linq;

using NUnit.Framework;

using Mono.Cecil.Rocks;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class MethodDefinitionRocksTests : BaseTestFixture {

		abstract class Foo {
			public abstract void DoFoo ();
		}

		class Bar : Foo {
			public override void DoFoo ()
			{
			}
		}

		class Baz : Bar {
			public override void DoFoo ()
			{
			}
		}

		[Test]
		public void GetBaseMethod ()
		{
			var baz = typeof (Baz).ToDefinition ();
			var baz_dofoo = baz.GetMethod ("DoFoo");

			var @base = baz_dofoo.GetBaseMethod ();
			Assert.AreEqual ("Bar", @base.DeclaringType.Name);

			@base = @base.GetBaseMethod ();
			Assert.AreEqual ("Foo", @base.DeclaringType.Name);

			Assert.AreEqual (@base, @base.GetBaseMethod ());
		}

		[Test]
		public void GetOriginalBaseMethod ()
		{
			var baz = typeof (Baz).ToDefinition ();
			var baz_dofoo = baz.GetMethod ("DoFoo");

			var @base = baz_dofoo.GetOriginalBaseMethod ();
			Assert.AreEqual ("Foo", @base.DeclaringType.Name);
		}
	}
}
