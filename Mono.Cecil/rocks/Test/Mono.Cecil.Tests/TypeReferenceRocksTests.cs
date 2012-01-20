using System;

using Mono.Cecil.Rocks;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class TypeReferenceRocksTests {

		[Test]
		public void MakeArrayType ()
		{
			var @string = GetTypeReference (typeof (string));

			var string_array = @string.MakeArrayType ();

			Assert.IsInstanceOf (typeof (ArrayType), string_array);
			Assert.AreEqual (1, string_array.Rank);
		}

		[Test]
		public void MakeArrayTypeRank ()
		{
			var @string = GetTypeReference (typeof (string));

			var string_array = @string.MakeArrayType (3);

			Assert.IsInstanceOf (typeof (ArrayType), string_array);
			Assert.AreEqual (3, string_array.Rank);
		}

		[Test]
		public void MakePointerType ()
		{
			var @string = GetTypeReference (typeof (string));

			var string_ptr = @string.MakePointerType ();

			Assert.IsInstanceOf (typeof (PointerType), string_ptr);
		}

		[Test]
		public void MakeByReferenceType ()
		{
			var @string = GetTypeReference (typeof (string));

			var string_byref = @string.MakeByReferenceType ();

			Assert.IsInstanceOf (typeof (ByReferenceType), string_byref);
		}

		class OptionalModifier {}

		[Test]
		public void MakeOptionalModifierType ()
		{
			var @string = GetTypeReference (typeof (string));
			var modopt = GetTypeReference (typeof (OptionalModifier));

			var string_modopt = @string.MakeOptionalModifierType (modopt);

			Assert.IsInstanceOf (typeof (OptionalModifierType), string_modopt);
			Assert.AreEqual (modopt, string_modopt.ModifierType);
		}

		class RequiredModifier { }

		[Test]
		public void MakeRequiredModifierType ()
		{
			var @string = GetTypeReference (typeof (string));
			var modreq = GetTypeReference (typeof (RequiredModifierType));

			var string_modreq = @string.MakeRequiredModifierType (modreq);

			Assert.IsInstanceOf (typeof (RequiredModifierType), string_modreq);
			Assert.AreEqual (modreq, string_modreq.ModifierType);
		}

		[Test]
		public void MakePinnedType ()
		{
			var byte_array = GetTypeReference (typeof (byte []));

			var pinned_byte_array = byte_array.MakePinnedType ();

			Assert.IsInstanceOf (typeof (PinnedType), pinned_byte_array);
		}

		[Test]
		public void MakeSentinelType ()
		{
			var @string = GetTypeReference (typeof (string));

			var string_sentinel = @string.MakeSentinelType ();

			Assert.IsInstanceOf (typeof (SentinelType), string_sentinel);
		}

		class Foo<T1, T2> {}

		[Test]
		public void MakeGenericInstanceType ()
		{
			var foo = GetTypeReference (typeof (Foo<,>));
			var @string = GetTypeReference (typeof (string));
			var @int = GetTypeReference (typeof (int));

			var foo_string_int = foo.MakeGenericInstanceType (@string, @int);

			Assert.IsInstanceOf (typeof (GenericInstanceType), foo_string_int);
			Assert.AreEqual (2, foo_string_int.GenericArguments.Count);
			Assert.AreEqual (@string, foo_string_int.GenericArguments [0]);
			Assert.AreEqual (@int, foo_string_int.GenericArguments [1]);
		}

		static TypeReference GetTypeReference (Type type)
		{
			return ModuleDefinition.ReadModule (typeof (TypeReferenceRocksTests).Module.FullyQualifiedName).Import (type);
		}
	}
}