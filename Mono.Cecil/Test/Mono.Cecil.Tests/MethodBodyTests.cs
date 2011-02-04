using System;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class MethodBodyTests : BaseTestFixture {

		[TestIL ("hello.il")]
		public void MultiplyMethod (ModuleDefinition module)
		{
			var foo = module.GetType ("Foo");
			Assert.IsNotNull (foo);

			var bar = foo.GetMethod ("Bar");
			Assert.IsNotNull (bar);
			Assert.IsTrue (bar.IsIL);

			AssertCode (@"
	.locals init (System.Int32 V_0)
	IL_0000: ldarg.0
	IL_0001: ldarg.1
	IL_0002: mul
	IL_0003: stloc.0
	IL_0004: ldloc.0
	IL_0005: call System.Void Foo::Baz(System.Int32)
	IL_000a: ret
", bar);
		}

		[TestIL ("hello.il")]
		public void PrintStringEmpty (ModuleDefinition module)
		{
			var foo = module.GetType ("Foo");
			Assert.IsNotNull (foo);

			var print_empty = foo.GetMethod ("PrintEmpty");
			Assert.IsNotNull (print_empty);

			AssertCode (@"
	.locals ()
	IL_0000: ldsfld System.String System.String::Empty
	IL_0005: call System.Void System.Console::WriteLine(System.String)
	IL_000a: ret
", print_empty);
		}

		[TestModule ("libhello.dll")]
		public void Branch (ModuleDefinition module)
		{
			var lib = module.GetType ("Library");
			Assert.IsNotNull (lib);

			var method = lib.GetMethod ("GetHelloString");
			Assert.IsNotNull (method);

			AssertCode (@"
	.locals init (System.String V_0)
	IL_0000: nop
	IL_0001: ldstr ""hello world of tomorrow""
	IL_0006: stloc.0
	IL_0007: br.s IL_0009
	IL_0009: ldloc.0
	IL_000a: ret
", method);
		}

		[TestModule ("switch.exe")]
		public void Switch (ModuleDefinition module)
		{
			var program = module.GetType ("Program");
			Assert.IsNotNull (program);

			var method = program.GetMethod ("Main");
			Assert.IsNotNull (method);

			AssertCode (@"
	.locals init (System.Int32 V_0)
	IL_0000: ldarg.0
	IL_0001: ldlen
	IL_0002: conv.i4
	IL_0003: stloc.0
	IL_0004: ldloc.0
	IL_0005: ldc.i4.8
	IL_0006: bgt.s IL_0026
	IL_0008: ldloc.0
	IL_0009: ldc.i4.1
	IL_000a: sub
	IL_000b: switch (IL_0032, IL_0034, IL_0038, IL_0034)
	IL_0020: ldloc.0
	IL_0021: ldc.i4.8
	IL_0022: beq.s IL_0036
	IL_0024: br.s IL_0038
	IL_0026: ldloc.0
	IL_0027: ldc.i4.s 16
	IL_0029: beq.s IL_0036
	IL_002b: ldloc.0
	IL_002c: ldc.i4.s 32
	IL_002e: beq.s IL_0036
	IL_0030: br.s IL_0038
	IL_0032: ldc.i4.0
	IL_0033: ret
	IL_0034: ldc.i4.1
	IL_0035: ret
	IL_0036: ldc.i4.2
	IL_0037: ret
	IL_0038: ldc.i4.s 42
	IL_003a: ret
", method);
		}

		[TestIL ("methodspecs.il")]
		public void MethodSpec (ModuleDefinition module)
		{
			var tamtam = module.GetType ("Tamtam");

			var bar = tamtam.GetMethod ("Bar");
			Assert.IsNotNull (bar);

			AssertCode (@"
	.locals ()
	IL_0000: ldc.i4.2
	IL_0001: call System.Void Tamtam::Foo<System.Int32>(TFoo)
	IL_0006: ret
", bar);
		}

		[TestModule ("catch.exe")]
		public void NestedTryCatchFinally (ModuleDefinition module)
		{
			var program = module.GetType ("Program");
			var main = program.GetMethod ("Main");
			Assert.IsNotNull (main);

			AssertCode (@"
	.locals ()
	IL_0000: call System.Void Program::Foo()
	IL_0005: leave.s IL_000d
	IL_0007: call System.Void Program::Baz()
	IL_000c: endfinally
	IL_000d: leave.s IL_001f
	IL_000f: pop
	IL_0010: call System.Void Program::Bar()
	IL_0015: leave.s IL_001f
	IL_0017: pop
	IL_0018: call System.Void Program::Bar()
	IL_001d: leave.s IL_001f
	IL_001f: leave.s IL_0027
	IL_0021: call System.Void Program::Baz()
	IL_0026: endfinally
	IL_0027: ret
	.try IL_0000 to IL_0007 finally handler IL_0007 to IL_000d
	.try IL_0000 to IL_000f catch System.ArgumentException handler IL_000f to IL_0017
	.try IL_0000 to IL_000f catch System.Exception handler IL_0017 to IL_001f
	.try IL_0000 to IL_0021 finally handler IL_0021 to IL_0027
", main);
		}

		[TestModule ("fptr.exe", Verify = false)]
		public void FunctionPointersAndCallSites (ModuleDefinition module)
		{
			var type = module.Types [0];
			var start = type.GetMethod ("Start");
			Assert.IsNotNull (start);

			AssertCode (@"
	.locals init ()
	IL_0000: ldc.i4.1
	IL_0001: call method System.Int32 *(System.Int32) MakeDecision::Decide()
	IL_0006: calli System.Int32(System.Int32)
	IL_000b: call System.Void System.Console::WriteLine(System.Int32)
	IL_0010: ldc.i4.1
	IL_0011: call method System.Int32 *(System.Int32) MakeDecision::Decide()
	IL_0016: calli System.Int32(System.Int32)
	IL_001b: call System.Void System.Console::WriteLine(System.Int32)
	IL_0020: ldc.i4.1
	IL_0021: call method System.Int32 *(System.Int32) MakeDecision::Decide()
	IL_0026: calli System.Int32(System.Int32)
	IL_002b: call System.Void System.Console::WriteLine(System.Int32)
	IL_0030: ret
", start);
		}

		[TestIL ("hello.il")]
		public void ThisParameter (ModuleDefinition module)
		{
			var type = module.GetType ("Foo");
			var method = type.GetMethod ("Gazonk");

			Assert.IsNotNull (method);

			AssertCode (@"
	.locals ()
	IL_0000: ldarg 0
	IL_0004: pop
	IL_0005: ret
", method);

			Assert.AreEqual (method.Body.ThisParameter, method.Body.Instructions [0].Operand);
		}

		[TestIL ("hello.il")]
		public void FilterMaxStack (ModuleDefinition module)
		{
			var type = module.GetType ("Foo");
			var method = type.GetMethod ("TestFilter");

			Assert.IsNotNull (method);
			Assert.AreEqual (2, method.Body.MaxStackSize);
		}

		[TestModule ("iterator.exe")]
		public void Iterator (ModuleDefinition module)
		{
			var method = module.GetType ("Program").GetMethod ("GetLittleArgs");
			Assert.IsNotNull (method.Body);
		}

		[TestCSharp ("CustomAttributes.cs")]
		public void LoadString (ModuleDefinition module)
		{
			var type = module.GetType ("FooAttribute");
			var get_fiou = type.GetMethod ("get_Fiou");
			Assert.IsNotNull (get_fiou);

			var ldstr = get_fiou.Body.Instructions.Where (i => i.OpCode == OpCodes.Ldstr).First ();
			Assert.AreEqual ("fiou", ldstr.Operand);
		}

		static void AssertCode (string expected, MethodDefinition method)
		{
			Assert.IsTrue (method.HasBody);
			Assert.IsNotNull (method.Body);

			Assert.AreEqual (Normalize (expected), Normalize (Formatter.FormatMethodBody (method)));
		}

		static string Normalize (string str)
		{
			return str.Trim ().Replace ("\r\n", "\n");
		}

		[Test]
		public void AddInstruction ()
		{
			var object_ref = new TypeReference ("System", "Object", null, null, false);
			var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
			var body = new MethodBody (method);

			var il = body.GetILProcessor ();

			var first = il.Create (OpCodes.Nop);
			var second = il.Create (OpCodes.Nop);

			body.Instructions.Add (first);
			body.Instructions.Add (second);

			Assert.IsNull (first.Previous);
			Assert.AreEqual (second, first.Next);
			Assert.AreEqual (first, second.Previous);
			Assert.IsNull (second.Next);
		}

		[Test]
		public void InsertInstruction ()
		{
			var object_ref = new TypeReference ("System", "Object", null, null, false);
			var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
			var body = new MethodBody (method);

			var il = body.GetILProcessor ();

			var first = il.Create (OpCodes.Nop);
			var second = il.Create (OpCodes.Nop);
			var third = il.Create (OpCodes.Nop);

			body.Instructions.Add (first);
			body.Instructions.Add (third);

			Assert.IsNull (first.Previous);
			Assert.AreEqual (third, first.Next);
			Assert.AreEqual (first, third.Previous);
			Assert.IsNull (third.Next);

			body.Instructions.Insert (1, second);

			Assert.IsNull (first.Previous);
			Assert.AreEqual (second, first.Next);
			Assert.AreEqual (first, second.Previous);
			Assert.AreEqual (third, second.Next);
			Assert.AreEqual (second, third.Previous);
			Assert.IsNull (third.Next);
		}

		[Test]
		public void InsertAfterLastInstruction ()
		{
			var object_ref = new TypeReference ("System", "Object", null, null, false);
			var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
			var body = new MethodBody (method);

			var il = body.GetILProcessor ();

			var first = il.Create (OpCodes.Nop);
			var second = il.Create (OpCodes.Nop);
			var third = il.Create (OpCodes.Nop);

			body.Instructions.Add (first);
			body.Instructions.Add (second);

			Assert.IsNull (first.Previous);
			Assert.AreEqual (second, first.Next);
			Assert.AreEqual (first, second.Previous);
			Assert.IsNull (second.Next);

			body.Instructions.Insert (2, third);

			Assert.IsNull (first.Previous);
			Assert.AreEqual (second, first.Next);
			Assert.AreEqual (first, second.Previous);
			Assert.AreEqual (third, second.Next);
			Assert.AreEqual (second, third.Previous);
			Assert.IsNull (third.Next);
		}

		[Test]
		public void RemoveInstruction ()
		{
			var object_ref = new TypeReference ("System", "Object", null, null, false);
			var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
			var body = new MethodBody (method);

			var il = body.GetILProcessor ();

			var first = il.Create (OpCodes.Nop);
			var second = il.Create (OpCodes.Nop);
			var third = il.Create (OpCodes.Nop);

			body.Instructions.Add (first);
			body.Instructions.Add (second);
			body.Instructions.Add (third);

			Assert.IsNull (first.Previous);
			Assert.AreEqual (second, first.Next);
			Assert.AreEqual (first, second.Previous);
			Assert.AreEqual (third, second.Next);
			Assert.AreEqual (second, third.Previous);
			Assert.IsNull (third.Next);

			body.Instructions.Remove (second);

			Assert.IsNull (first.Previous);
			Assert.AreEqual (third, first.Next);
			Assert.AreEqual (first, third.Previous);
			Assert.IsNull (third.Next);
		}
	}
}
