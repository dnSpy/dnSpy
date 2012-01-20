using System;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Metadata;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class MethodTests : BaseTestFixture {

		[TestCSharp ("Methods.cs")]
		public void AbstractMethod (ModuleDefinition module)
		{
			var type = module.Types [1];
			Assert.AreEqual ("Foo", type.Name);
			Assert.AreEqual (2, type.Methods.Count);

			var method = type.GetMethod ("Bar");
			Assert.AreEqual ("Bar", method.Name);
			Assert.IsTrue (method.IsAbstract);
			Assert.IsNotNull (method.ReturnType);

			Assert.AreEqual (1, method.Parameters.Count);

			var parameter = method.Parameters [0];

			Assert.AreEqual ("a", parameter.Name);
			Assert.AreEqual ("System.Int32", parameter.ParameterType.FullName);
		}

		[TestCSharp ("Methods.cs")]
		public void SimplePInvoke (ModuleDefinition module)
		{
			var bar = module.GetType ("Bar");
			var pan = bar.GetMethod ("Pan");

			Assert.IsTrue (pan.IsPInvokeImpl);
			Assert.IsNotNull (pan.PInvokeInfo);

			Assert.AreEqual ("Pan", pan.PInvokeInfo.EntryPoint);
			Assert.IsNotNull (pan.PInvokeInfo.Module);
			Assert.AreEqual ("foo.dll", pan.PInvokeInfo.Module.Name);
		}

		[TestCSharp ("Generics.cs")]
		public void GenericMethodDefinition (ModuleDefinition module)
		{
			var baz = module.GetType ("Baz");

			var gazonk = baz.GetMethod ("Gazonk");

			Assert.IsNotNull (gazonk);

			Assert.IsTrue (gazonk.HasGenericParameters);
			Assert.AreEqual (1, gazonk.GenericParameters.Count);
			Assert.AreEqual ("TBang", gazonk.GenericParameters [0].Name);
		}

		[TestCSharp ("Generics.cs")]
		public void ReturnGenericInstance (ModuleDefinition module)
		{
			var bar = module.GetType ("Bar`1");

			var self = bar.GetMethod ("Self");
			Assert.IsNotNull (self);

			var bar_t = self.ReturnType;

			Assert.IsTrue (bar_t.IsGenericInstance);

			var bar_t_instance = (GenericInstanceType) bar_t;

			Assert.AreEqual (bar.GenericParameters [0], bar_t_instance.GenericArguments [0]);

			var self_str = bar.GetMethod ("SelfString");
			Assert.IsNotNull (self_str);

			var bar_str = self_str.ReturnType;
			Assert.IsTrue (bar_str.IsGenericInstance);

			var bar_str_instance = (GenericInstanceType) bar_str;

			Assert.AreEqual ("System.String", bar_str_instance.GenericArguments [0].FullName);
		}

		[TestCSharp ("Generics.cs")]
		public void ReturnGenericInstanceWithMethodParameter (ModuleDefinition module)
		{
			var baz = module.GetType ("Baz");

			var gazoo = baz.GetMethod ("Gazoo");
			Assert.IsNotNull (gazoo);

			var bar_bingo = gazoo.ReturnType;

			Assert.IsTrue (bar_bingo.IsGenericInstance);

			var bar_bingo_instance = (GenericInstanceType) bar_bingo;

			Assert.AreEqual (gazoo.GenericParameters [0], bar_bingo_instance.GenericArguments [0]);
		}

		[TestCSharp ("Interfaces.cs")]
		public void SimpleOverrides (ModuleDefinition module)
		{
			var ibingo = module.GetType ("IBingo");
			var ibingo_foo = ibingo.GetMethod ("Foo");
			Assert.IsNotNull (ibingo_foo);

			var ibingo_bar = ibingo.GetMethod ("Bar");
			Assert.IsNotNull (ibingo_bar);

			var bingo = module.GetType ("Bingo");

			var foo = bingo.GetMethod ("IBingo.Foo");
			Assert.IsNotNull (foo);

			Assert.IsTrue (foo.HasOverrides);
			Assert.AreEqual (ibingo_foo, foo.Overrides [0]);

			var bar = bingo.GetMethod ("IBingo.Bar");
			Assert.IsNotNull (bar);

			Assert.IsTrue (bar.HasOverrides);
			Assert.AreEqual (ibingo_bar, bar.Overrides [0]);
		}

		[TestModule ("varargs.exe")]
		public void VarArgs (ModuleDefinition module)
		{
			var module_type = module.Types [0];

			Assert.AreEqual (3, module_type.Methods.Count);

			var bar = module_type.GetMethod ("Bar");
			var baz = module_type.GetMethod ("Baz");
			var foo = module_type.GetMethod ("Foo");

			Assert.IsTrue (bar.IsVarArg ());
			Assert.IsFalse (baz.IsVarArg ());

			Assert.IsTrue(foo.IsVarArg ());

			var bar_reference = (MethodReference) baz.Body.Instructions.Where (i => i.Offset == 0x000a).First ().Operand;

			Assert.IsTrue (bar_reference.IsVarArg ());
			Assert.AreEqual (0, bar_reference.GetSentinelPosition ());

			var foo_reference = (MethodReference) baz.Body.Instructions.Where (i => i.Offset == 0x0023).First ().Operand;

			Assert.IsTrue (foo_reference.IsVarArg ());

			Assert.AreEqual (1, foo_reference.GetSentinelPosition ());
		}

		[TestCSharp ("Generics.cs")]
		public void GenericInstanceMethod (ModuleDefinition module)
		{
			var type = module.GetType ("It");
			var method = type.GetMethod ("ReadPwow");

			GenericInstanceMethod instance = null;

			foreach (var instruction in method.Body.Instructions) {
				instance = instruction.Operand as GenericInstanceMethod;
				if (instance != null)
					break;
			}

			Assert.IsNotNull (instance);

			Assert.AreEqual (TokenType.MethodSpec, instance.MetadataToken.TokenType);
			Assert.AreNotEqual (0, instance.MetadataToken.RID);
		}

		[TestCSharp ("Generics.cs")]
		public void MethodRefDeclaredOnGenerics (ModuleDefinition module)
		{
			var type = module.GetType ("Tamtam");
			var beta = type.GetMethod ("Beta");
			var charlie = type.GetMethod ("Charlie");

			var new_list_beta = (MethodReference) beta.Body.Instructions [0].Operand;
			var new_list_charlie = (MethodReference) charlie.Body.Instructions [0].Operand;

			Assert.AreEqual ("System.Collections.Generic.List`1<TBeta>", new_list_beta.DeclaringType.FullName);
			Assert.AreEqual ("System.Collections.Generic.List`1<TCharlie>", new_list_charlie.DeclaringType.FullName);
		}

		[Test]
		public void ReturnParameterMethod ()
		{
			var method = typeof (MethodTests).ToDefinition ().GetMethod ("ReturnParameterMethod");
			Assert.IsNotNull (method);
			Assert.AreEqual (method, method.MethodReturnType.Parameter.Method);
		}
	}
}
