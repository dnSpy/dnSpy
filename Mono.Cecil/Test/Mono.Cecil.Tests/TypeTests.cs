using System;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class TypeTests : BaseTestFixture {

		[TestCSharp ("Layouts.cs")]
		public void TypeLayout (ModuleDefinition module)
		{
			var foo = module.GetType ("Foo");
			Assert.IsNotNull (foo);
			Assert.IsTrue (foo.IsValueType);

			Assert.IsTrue (foo.HasLayoutInfo);
			Assert.AreEqual (16, foo.ClassSize);

			var babar = module.GetType ("Babar");
			Assert.IsNotNull (babar);
			Assert.IsFalse (babar.IsValueType);
			Assert.IsFalse (babar.HasLayoutInfo);
		}

		[TestIL ("types.il")]
		public void SimpleInterfaces (ModuleDefinition module)
		{
			var ibaz = module.GetType ("IBaz");
			Assert.IsNotNull (ibaz);

			Assert.IsTrue (ibaz.HasInterfaces);

			var interfaces = ibaz.Interfaces;

			Assert.AreEqual (2, interfaces.Count);

			Assert.AreEqual ("IBar", interfaces [0].FullName);
			Assert.AreEqual ("IFoo", interfaces [1].FullName);
		}

		[TestCSharp ("Generics.cs")]
		public void GenericTypeDefinition (ModuleDefinition module)
		{
			var foo = module.GetType ("Foo`2");
			Assert.IsNotNull (foo);

			Assert.IsTrue (foo.HasGenericParameters);
			Assert.AreEqual (2, foo.GenericParameters.Count);

			var tbar = foo.GenericParameters [0];

			Assert.AreEqual ("TBar", tbar.Name);
			Assert.AreEqual (foo, tbar.Owner);

			var tbaz = foo.GenericParameters [1];

			Assert.AreEqual ("TBaz", tbaz.Name);
			Assert.AreEqual (foo, tbaz.Owner);
		}

		[TestCSharp ("Generics.cs")]
		public void ConstrainedGenericType (ModuleDefinition module)
		{
			var bongo_t = module.GetType ("Bongo`1");
			Assert.IsNotNull (bongo_t);

			var t = bongo_t.GenericParameters [0];
			Assert.IsNotNull (t);
			Assert.AreEqual ("T", t.Name);

			Assert.IsTrue (t.HasConstraints);
			Assert.AreEqual (2, t.Constraints.Count);

			Assert.AreEqual ("Zap", t.Constraints [0].FullName);
			Assert.AreEqual ("IZoom", t.Constraints [1].FullName);
		}

		[TestCSharp ("Generics.cs")]
		public void GenericBaseType (ModuleDefinition module)
		{
			var child = module.GetType ("Child`1");

			var child_t = child.GenericParameters [0];
			Assert.IsNotNull (child_t);

			var instance = child.BaseType as GenericInstanceType;
			Assert.IsNotNull (instance);
			Assert.AreNotEqual (0, instance.MetadataToken.RID);

			Assert.AreEqual (child_t, instance.GenericArguments [0]);
		}

		[TestCSharp ("Generics.cs")]
		public void GenericConstraintOnGenericParameter (ModuleDefinition module)
		{
			var duel = module.GetType ("Duel`3");

			Assert.AreEqual (3, duel.GenericParameters.Count);

			var t1 = duel.GenericParameters [0];
			var t2 = duel.GenericParameters [1];
			var t3 = duel.GenericParameters [2];

			Assert.AreEqual (t1, t2.Constraints [0]);
			Assert.AreEqual (t2, t3.Constraints [0]);
		}

		[TestCSharp ("Generics.cs")]
		public void GenericForwardBaseType (ModuleDefinition module)
		{
			var tamchild = module.GetType ("TamChild");

			Assert.IsNotNull (tamchild);
			Assert.IsNotNull (tamchild.BaseType);

			var generic_instance = tamchild.BaseType as GenericInstanceType;

			Assert.IsNotNull (generic_instance);

			Assert.AreEqual (1, generic_instance.GenericArguments.Count);
			Assert.AreEqual (module.GetType ("Tamtam"), generic_instance.GenericArguments [0]);
		}

		[TestCSharp ("Generics.cs")]
		public void TypeExtentingGenericOfSelf (ModuleDefinition module)
		{
			var rec_child = module.GetType ("RecChild");

			Assert.IsNotNull (rec_child);
			Assert.IsNotNull (rec_child.BaseType);

			var generic_instance = rec_child.BaseType as GenericInstanceType;

			Assert.IsNotNull (generic_instance);

			Assert.AreEqual (1, generic_instance.GenericArguments.Count);
			Assert.AreEqual (rec_child, generic_instance.GenericArguments [0]);
		}

		[TestCSharp ("Methods.cs")]
		public void TypeReferenceValueType (ModuleDefinition module)
		{
			var baz = module.GetType ("Baz");
			var method = baz.GetMethod ("PrintAnswer");

			var box = method.Body.Instructions.Where (i => i.OpCode == OpCodes.Box).First ();
			var int32 = (TypeReference) box.Operand;

			Assert.IsTrue (int32.IsValueType);
		}

		[TestModule ("gifaceref.exe")]
		public void GenericInterfaceReference (ModuleDefinition module)
		{
			var type = module.GetType ("Program");
			var iface = type.Interfaces [0];

			var instance = (GenericInstanceType) iface;
			var owner = instance.ElementType;

			Assert.AreEqual (1, instance.GenericArguments.Count);
			Assert.AreEqual (1, owner.GenericParameters.Count);
		}

		[TestModule ("cscgpbug.dll", Verify = false)]
		public void UnboundGenericParameter (ModuleDefinition module)
		{
			var type = module.GetType ("ListViewModel");
			var method = type.GetMethod ("<>n__FabricatedMethod1");

			var parameter = method.ReturnType as GenericParameter;

			Assert.IsNotNull (parameter);
			Assert.AreEqual (0, parameter.Position);
			Assert.IsNull (parameter.Owner);
		}

		[TestCSharp ("Generics.cs")]
		public void GenericMultidimensionalArray (ModuleDefinition module)
		{
			var type = module.GetType ("LaMatrix");
			var method = type.GetMethod ("At");

			var call = method.Body.Instructions.Where (i => i.Operand is MethodReference).First ();
			var get = (MethodReference) call.Operand;

			Assert.IsNotNull (get);
			Assert.AreEqual (0, get.GenericParameters.Count);
			Assert.AreEqual (MethodCallingConvention.Default, get.CallingConvention);
			Assert.AreEqual (method.GenericParameters [0], get.ReturnType);
		}

		[Test]
		public void CorlibPrimitive ()
		{
			var module = typeof (TypeTests).ToDefinition ().Module;

			var int32 = module.TypeSystem.Int32;
			Assert.IsTrue (int32.IsPrimitive);
			Assert.AreEqual (MetadataType.Int32, int32.MetadataType);

			var int32_def = int32.Resolve ();
			Assert.IsTrue (int32_def.IsPrimitive);
			Assert.AreEqual (MetadataType.Int32, int32_def.MetadataType);
		}

		[TestIL ("explicitthis.il", Verify = false)]
		public void ExplicitThis (ModuleDefinition module)
		{
			var type = module.GetType ("MakeDecision");
			var method = type.GetMethod ("Decide");
			var fptr = method.ReturnType as FunctionPointerType;

			Assert.IsNotNull (fptr);
			Assert.IsTrue (fptr.HasThis);
			Assert.IsTrue (fptr.ExplicitThis);

			Assert.AreEqual (0, fptr.Parameters [0].Sequence);
			Assert.AreEqual (1, fptr.Parameters [1].Sequence);
		}
	}
}
