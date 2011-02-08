using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Rocks;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class TypeDefinitionRocksTests {

		class Foo {

			static Foo ()
			{
			}

			public Foo (int a)
			{
			}

			public Foo (int a, string s)
			{
			}

			public static void Bar ()
			{
			}

			void Baz ()
			{
			}
		}

		[Test]
		public void GetConstructors ()
		{
			var foo = typeof (Foo).ToDefinition ();
			var ctors = foo.GetConstructors ().Select (ctor => ctor.FullName);

			var expected = new [] {
				"System.Void Mono.Cecil.Tests.TypeDefinitionRocksTests/Foo::.cctor()",
				"System.Void Mono.Cecil.Tests.TypeDefinitionRocksTests/Foo::.ctor(System.Int32)",
				"System.Void Mono.Cecil.Tests.TypeDefinitionRocksTests/Foo::.ctor(System.Int32,System.String)",
			};

			AssertSet (expected, ctors);
		}

		static void AssertSet<T> (IEnumerable<T> expected, IEnumerable<T> actual)
		{
			Assert.IsFalse (expected.Except (actual).Any ());
			Assert.IsTrue (expected.Intersect (actual).SequenceEqual (expected));
		}

		[Test]
		public void GetStaticConstructor ()
		{
			var foo = typeof (Foo).ToDefinition ();
			var cctor = foo.GetStaticConstructor ();

			Assert.IsNotNull (cctor);
			Assert.AreEqual ("System.Void Mono.Cecil.Tests.TypeDefinitionRocksTests/Foo::.cctor()", cctor.FullName);
		}

		[Test]
		public void GetMethods ()
		{
			var foo = typeof (Foo).ToDefinition ();
			var methods = foo.GetMethods ().ToArray ();

			Assert.AreEqual (2, methods.Length);
			Assert.AreEqual ("System.Void Mono.Cecil.Tests.TypeDefinitionRocksTests/Foo::Bar()", methods [0].FullName);
			Assert.AreEqual ("System.Void Mono.Cecil.Tests.TypeDefinitionRocksTests/Foo::Baz()", methods [1].FullName);
		}

		enum Pan : byte {
			Pin,
			Pon,
		}

		[Test]
		public void GetEnumUnderlyingType ()
		{
			var pan = typeof (Pan).ToDefinition ();

			Assert.IsNotNull (pan);
			Assert.IsTrue (pan.IsEnum);

			var underlying_type = pan.GetEnumUnderlyingType ();
			Assert.IsNotNull (underlying_type);

			Assert.AreEqual ("System.Byte", underlying_type.FullName);
		}
	}
}