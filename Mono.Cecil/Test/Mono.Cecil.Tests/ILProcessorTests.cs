using System;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class ILProcessorTests : BaseTestFixture {

		[Test]
		public void Append ()
		{
			var method = CreateTestMethod ();
			var il = method.GetILProcessor ();

			var ret = il.Create (OpCodes.Ret);
			il.Append (ret);

			AssertOpCodeSequence (new [] { OpCodes.Ret }, method);
		}

		[Test]
		public void InsertBefore ()
		{
			var method = CreateTestMethod (OpCodes.Ldloc_0, OpCodes.Ldloc_2, OpCodes.Ldloc_3);
			var il = method.GetILProcessor ();

			var ldloc_2 = method.Instructions.Where (i => i.OpCode == OpCodes.Ldloc_2).First ();

			il.InsertBefore (
				ldloc_2,
				il.Create (OpCodes.Ldloc_1));

			AssertOpCodeSequence (new [] { OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3 }, method);
		}

		[Test]
		public void InsertAfter ()
		{
			var method = CreateTestMethod (OpCodes.Ldloc_0, OpCodes.Ldloc_2, OpCodes.Ldloc_3);
			var il = method.GetILProcessor ();

			var ldloc_0 = method.Instructions.First ();

			il.InsertAfter (
				ldloc_0,
				il.Create (OpCodes.Ldloc_1));

			AssertOpCodeSequence (new [] { OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3 }, method);
		}

		static void AssertOpCodeSequence (OpCode [] expected, MethodBody body)
		{
			var opcodes = body.Instructions.Select (i => i.OpCode).ToArray ();
			Assert.AreEqual (expected.Length, opcodes.Length);

			for (int i = 0; i < opcodes.Length; i++)
				Assert.AreEqual (expected [i], opcodes [i]);
		}

		static MethodBody CreateTestMethod (params OpCode [] opcodes)
		{
			var method = new MethodDefinition {
				Name = "function",
			};

			var il = method.Body.GetILProcessor ();

			foreach (var opcode in opcodes)
				il.Emit (opcode);

			return method.Body;
		}
	}
}
