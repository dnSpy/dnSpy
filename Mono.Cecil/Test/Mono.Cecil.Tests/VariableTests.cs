using System;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class VariableTests : BaseTestFixture {

		[Test]
		public void AddVariableIndex ()
		{
			var object_ref = new TypeReference ("System", "Object", null, null, false);
			var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
			var body = new MethodBody (method);

			var x = new VariableDefinition ("x", object_ref);
			var y = new VariableDefinition ("y", object_ref);

			body.Variables.Add (x);
			body.Variables.Add (y);

			Assert.AreEqual (0, x.Index);
			Assert.AreEqual (1, y.Index);
		}

		[Test]
		public void RemoveAtVariableIndex ()
		{
			var object_ref = new TypeReference ("System", "Object", null, null, false);
			var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
			var body = new MethodBody (method);

			var x = new VariableDefinition ("x", object_ref);
			var y = new VariableDefinition ("y", object_ref);
			var z = new VariableDefinition ("z", object_ref);

			body.Variables.Add (x);
			body.Variables.Add (y);
			body.Variables.Add (z);

			Assert.AreEqual (0, x.Index);
			Assert.AreEqual (1, y.Index);
			Assert.AreEqual (2, z.Index);

			body.Variables.RemoveAt (1);

			Assert.AreEqual (0, x.Index);
			Assert.AreEqual (-1, y.Index);
			Assert.AreEqual (1, z.Index);
		}

		[Test]
		public void RemoveVariableIndex ()
		{
			var object_ref = new TypeReference ("System", "Object", null, null, false);
			var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
			var body = new MethodBody (method);

			var x = new VariableDefinition ("x", object_ref);
			var y = new VariableDefinition ("y", object_ref);
			var z = new VariableDefinition ("z", object_ref);

			body.Variables.Add (x);
			body.Variables.Add (y);
			body.Variables.Add (z);

			Assert.AreEqual (0, x.Index);
			Assert.AreEqual (1, y.Index);
			Assert.AreEqual (2, z.Index);

			body.Variables.Remove (y);

			Assert.AreEqual (0, x.Index);
			Assert.AreEqual (-1, y.Index);
			Assert.AreEqual (1, z.Index);
		}

		[Test]
		public void InsertVariableIndex ()
		{
			var object_ref = new TypeReference ("System", "Object", null, null, false);
			var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
			var body = new MethodBody (method);

			var x = new VariableDefinition ("x", object_ref);
			var y = new VariableDefinition ("y", object_ref);
			var z = new VariableDefinition ("z", object_ref);

			body.Variables.Add (x);
			body.Variables.Add (z);

			Assert.AreEqual (0, x.Index);
			Assert.AreEqual (-1, y.Index);
			Assert.AreEqual (1, z.Index);

			body.Variables.Insert (1, y);

			Assert.AreEqual (0, x.Index);
			Assert.AreEqual (1, y.Index);
			Assert.AreEqual (2, z.Index);
		}
	}
}
