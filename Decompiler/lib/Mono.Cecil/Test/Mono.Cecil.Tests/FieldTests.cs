using System;

using Mono.Cecil;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class FieldTests : BaseTestFixture {

		[TestCSharp ("Fields.cs")]
		public void TypeDefField (ModuleDefinition module)
		{
			var type = module.Types [1];
			Assert.AreEqual ("Foo", type.Name);
			Assert.AreEqual (1, type.Fields.Count);

			var field = type.Fields [0];
			Assert.AreEqual ("bar", field.Name);
			Assert.AreEqual (1, field.MetadataToken.RID);
			Assert.IsNotNull (field.FieldType);
			Assert.AreEqual ("Bar", field.FieldType.FullName);
			Assert.AreEqual (TokenType.Field, field.MetadataToken.TokenType);
			Assert.IsFalse (field.HasConstant);
			Assert.IsNull (field.Constant);
		}

		[TestCSharp ("Fields.cs")]
		public void PrimitiveTypes (ModuleDefinition module)
		{
			var type = module.GetType ("Baz");

			AssertField (type, "char", typeof (char));
			AssertField (type, "bool", typeof (bool));
			AssertField (type, "sbyte", typeof (sbyte));
			AssertField (type, "byte", typeof (byte));
			AssertField (type, "int16", typeof (short));
			AssertField (type, "uint16", typeof (ushort));
			AssertField (type, "int32", typeof (int));
			AssertField (type, "uint32", typeof (uint));
			AssertField (type, "int64", typeof (long));
			AssertField (type, "uint64", typeof (ulong));
			AssertField (type, "single", typeof (float));
			AssertField (type, "double", typeof (double));
			AssertField (type, "string", typeof (string));
			AssertField (type, "object", typeof (object));
		}

		[TestCSharp ("Fields.cs")]
		public void VolatileField (ModuleDefinition module)
		{
			var type = module.GetType ("Bar");

			Assert.IsTrue (type.HasFields);
			Assert.AreEqual (1, type.Fields.Count);

			var field = type.Fields [0];

			Assert.AreEqual ("oiseau", field.Name);
			Assert.AreEqual ("System.Int32 modreq(System.Runtime.CompilerServices.IsVolatile)", field.FieldType.FullName);

			Assert.IsFalse (field.HasConstant);
		}

		[TestCSharp ("Layouts.cs")]
		public void FieldLayout (ModuleDefinition module)
		{
			var foo = module.GetType ("Foo");
			Assert.IsNotNull (foo);

			Assert.IsTrue (foo.HasFields);

			var fields = foo.Fields;

			var field = fields [0];

			Assert.AreEqual ("Bar", field.Name);
			Assert.IsTrue (field.HasLayoutInfo);
			Assert.AreEqual (0, field.Offset);

			field = fields [1];

			Assert.AreEqual ("Baz", field.Name);
			Assert.IsTrue (field.HasLayoutInfo);
			Assert.AreEqual (2, field.Offset);

			field = fields [2];

			Assert.AreEqual ("Gazonk", field.Name);
			Assert.IsTrue (field.HasLayoutInfo);
			Assert.AreEqual (4, field.Offset);
		}

		[TestCSharp ("Layouts.cs")]
		public void FieldRVA (ModuleDefinition module)
		{
			var priv_impl = GetPrivateImplementationType (module);
			Assert.IsNotNull (priv_impl);

			Assert.AreEqual (1, priv_impl.Fields.Count);

			var field = priv_impl.Fields [0];

			Assert.IsNotNull (field);
			Assert.AreNotEqual (0, field.RVA);
			Assert.IsNotNull (field.InitialValue);
			Assert.AreEqual (16, field.InitialValue.Length);

			var buffer = new ByteBuffer (field.InitialValue);

			Assert.AreEqual (1, buffer.ReadUInt32 ());
			Assert.AreEqual (2, buffer.ReadUInt32 ());
			Assert.AreEqual (3, buffer.ReadUInt32 ());
			Assert.AreEqual (4, buffer.ReadUInt32 ());
		}

		[TestCSharp ("Generics.cs")]
		public void GenericFieldDefinition (ModuleDefinition module)
		{
			var bar = module.GetType ("Bar`1");
			Assert.IsNotNull (bar);

			Assert.IsTrue (bar.HasGenericParameters);
			var t = bar.GenericParameters [0];

			Assert.AreEqual ("T", t.Name);
			Assert.AreEqual (t.Owner, bar);

			var bang = bar.GetField ("bang");

			Assert.IsNotNull (bang);

			Assert.AreEqual (t, bang.FieldType);
		}

		[TestIL ("types.il")]
		public void ArrayFields (ModuleDefinition module)
		{
			var types = module.GetType ("Types");
			Assert.IsNotNull (types);

			var rank_two = types.GetField ("rank_two");

			var array = rank_two.FieldType as ArrayType;
			Assert.IsNotNull (array);

			Assert.AreEqual (2, array.Rank);
			Assert.IsFalse (array.Dimensions [0].IsSized);
			Assert.IsFalse (array.Dimensions [1].IsSized);

			var rank_two_low_bound_zero = types.GetField ("rank_two_low_bound_zero");

			array = rank_two_low_bound_zero.FieldType as ArrayType;
			Assert.IsNotNull (array);

			Assert.AreEqual (2, array.Rank);
			Assert.IsTrue (array.Dimensions [0].IsSized);
			Assert.AreEqual (0, array.Dimensions [0].LowerBound);
			Assert.AreEqual (null, array.Dimensions [0].UpperBound);
			Assert.IsTrue (array.Dimensions [1].IsSized);
			Assert.AreEqual (0, array.Dimensions [1].LowerBound);
			Assert.AreEqual (null, array.Dimensions [1].UpperBound);
		}

		[TestCSharp ("Fields.cs")]
		public void EnumFieldsConstant (ModuleDefinition module)
		{
			var pim = module.GetType ("Pim");
			Assert.IsNotNull (pim);

			var field = pim.GetField ("Pam");
			Assert.IsTrue (field.HasConstant);
			Assert.AreEqual (1, (int) field.Constant);

			field = pim.GetField ("Poum");
			Assert.AreEqual (2, (int) field.Constant);
		}

		[TestCSharp ("Fields.cs")]
		public void StringAndClassConstant (ModuleDefinition module)
		{
			var panpan = module.GetType ("PanPan");
			Assert.IsNotNull (panpan);

			var field = panpan.GetField ("Peter");
			Assert.IsTrue (field.HasConstant);
			Assert.IsNull (field.Constant);

			field = panpan.GetField ("QQ");
			Assert.AreEqual ("qq", (string) field.Constant);

			field = panpan.GetField ("nil");
			Assert.AreEqual (null, (string) field.Constant);
		}

		[TestCSharp ("Fields.cs")]
		public void ObjectConstant (ModuleDefinition module)
		{
			var panpan = module.GetType ("PanPan");
			Assert.IsNotNull (panpan);

			var field = panpan.GetField ("obj");
			Assert.IsTrue (field.HasConstant);
			Assert.IsNull (field.Constant);
		}

		[TestIL ("types.il")]
		public void NullPrimitiveConstant (ModuleDefinition module)
		{
			var fields = module.GetType ("Fields");

			var field = fields.GetField ("int32_nullref");
			Assert.IsTrue (field.HasConstant);
			Assert.AreEqual (null, field.Constant);
		}

		[TestCSharp ("Fields.cs")]
		public void ArrayConstant (ModuleDefinition module)
		{
			var panpan = module.GetType ("PanPan");
			Assert.IsNotNull (panpan);

			var field = panpan.GetField ("ints");
			Assert.IsTrue (field.HasConstant);
			Assert.IsNull (field.Constant);
		}

		[TestIL ("types.il")]
		public void ConstantCoalescing (ModuleDefinition module)
		{
			var fields = module.GetType ("Fields");

			var field = fields.GetField ("int32_int16");
			Assert.AreEqual ("System.Int32", field.FieldType.FullName);
			Assert.IsTrue (field.HasConstant);
			Assert.IsInstanceOfType (typeof (short), field.Constant);
			Assert.AreEqual ((short) 1, field.Constant);

			field = fields.GetField ("int16_int32");
			Assert.AreEqual ("System.Int16", field.FieldType.FullName);
			Assert.IsTrue (field.HasConstant);
			Assert.IsInstanceOfType (typeof (int), field.Constant);
			Assert.AreEqual (1, field.Constant);

			field = fields.GetField ("char_int16");
			Assert.AreEqual ("System.Char", field.FieldType.FullName);
			Assert.IsTrue (field.HasConstant);
			Assert.IsInstanceOfType (typeof (short), field.Constant);
			Assert.AreEqual ((short) 1, field.Constant);

			field = fields.GetField ("int16_char");
			Assert.AreEqual ("System.Int16", field.FieldType.FullName);
			Assert.IsTrue (field.HasConstant);
			Assert.IsInstanceOfType (typeof (char), field.Constant);
			Assert.AreEqual ('s', field.Constant);
		}

		[TestCSharp ("Generics.cs")]
		public void NestedEnumOfGenericTypeDefinition (ModuleDefinition module)
		{
			var dang = module.GetType ("Bongo`1/Dang");
			Assert.IsNotNull (dang);

			var field = dang.GetField ("Ding");
			Assert.IsNotNull (field);
			Assert.AreEqual (2, field.Constant);

			field = dang.GetField ("Dong");
			Assert.IsNotNull (field);
			Assert.AreEqual (12, field.Constant);
		}

		[TestModule ("marshal.dll")]
		public void MarshalAsFixedStr (ModuleDefinition module)
		{
			var boc = module.GetType ("Boc");
			var field = boc.GetField ("a");

			Assert.IsNotNull (field);

			Assert.IsTrue (field.HasMarshalInfo);

			var info = (FixedSysStringMarshalInfo) field.MarshalInfo;

			Assert.AreEqual (42, info.Size);
		}

		[TestModule ("marshal.dll")]
		public void MarshalAsFixedArray (ModuleDefinition module)
		{
			var boc = module.GetType ("Boc");
			var field = boc.GetField ("b");

			Assert.IsNotNull (field);

			Assert.IsTrue (field.HasMarshalInfo);

			var info = (FixedArrayMarshalInfo) field.MarshalInfo;

			Assert.AreEqual (12, info.Size);
			Assert.AreEqual (NativeType.Boolean, info.ElementType);
		}

		static TypeDefinition GetPrivateImplementationType (ModuleDefinition module)
		{
			foreach (var type in module.Types)
				if (type.FullName.Contains ("<PrivateImplementationDetails>"))
					return type;

			return null;
		}

		static void AssertField (TypeDefinition type, string name, Type expected)
		{
			var field = type.GetField (name);
			Assert.IsNotNull (field, name);

			Assert.AreEqual (expected.FullName, field.FieldType.FullName);
		}
	}
}
