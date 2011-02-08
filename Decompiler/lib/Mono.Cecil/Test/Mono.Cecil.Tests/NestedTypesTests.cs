using System;

using Mono.Cecil;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class NestedTypesTests : BaseTestFixture {

		[TestCSharp ("NestedTypes.cs")]
		public void NestedTypes (ModuleDefinition module)
		{
			var foo = module.GetType ("Foo");

			Assert.AreEqual ("Foo", foo.Name);
			Assert.AreEqual ("Foo", foo.FullName);
			Assert.AreEqual (module, foo.Module);
			Assert.AreEqual (1, foo.NestedTypes.Count);

			var bar = foo.NestedTypes [0];

			Assert.AreEqual ("Bar", bar.Name);
			Assert.AreEqual ("Foo/Bar", bar.FullName);
			Assert.AreEqual (module, bar.Module);
			Assert.AreEqual (1, bar.NestedTypes.Count);

			var baz = bar.NestedTypes [0];

			Assert.AreEqual ("Baz", baz.Name);
			Assert.AreEqual ("Foo/Bar/Baz", baz.FullName);
			Assert.AreEqual (module, baz.Module);
		}

		[TestCSharp ("NestedTypes.cs")]
		public void DirectNestedType (ModuleDefinition module)
		{
			var bingo = module.GetType ("Bingo");
			var get_fuel = bingo.GetMethod ("GetFuel");

			Assert.AreEqual ("Bingo/Fuel", get_fuel.ReturnType.FullName);
		}
	}
}
