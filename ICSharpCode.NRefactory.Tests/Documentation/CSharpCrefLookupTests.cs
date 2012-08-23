// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.Documentation
{
	[TestFixture]
	public class CSharpCrefLookupTests
	{
		IEntity Lookup(string cref)
		{
			string program = @"using System;
using System.Collections.Generic;
/// <summary/>
class Test {
	int @int;
	void M(int a) {}
	
	void Overloaded(int a) {}
	void Overloaded(string a) {}
	void Overloaded(ref int a) {}
	
	public int this[int index] { get { return 0; } }
	
	public static int operator +(Test a, int b) { return 0; }
	public static implicit operator Test(int a) { return 0; }
	public static implicit operator int(Test a) { return 0; }
}
interface IGeneric<A, B> {
	void Test<T>(ref T[,] a);
}
class Impl<T> : IGeneric<List<string>[,], T> {
	void IGeneric<List<string>[,], T>.Test<X>(ref X[,] a) {}
}";
			
			var pc = new CSharpProjectContent().AddAssemblyReferences(new[] { CecilLoaderTests.Mscorlib });
			var syntaxTree = SyntaxTree.Parse(program, "program.cs");
			var compilation = pc.AddOrUpdateFiles(syntaxTree.ToTypeSystem()).CreateCompilation();
			var typeDefinition = compilation.MainAssembly.TopLevelTypeDefinitions.First();
			IEntity entity = typeDefinition.Documentation.ResolveCref(cref);
			Assert.IsNotNull(entity, "ResolveCref() returned null.");
			return entity;
		}
		
		[Test]
		public void String()
		{
			Assert.AreEqual("System.String",
			                Lookup("string").ReflectionName);
		}
		
		[Test]
		public void IntParse()
		{
			Assert.AreEqual("M:System.Int32.Parse(System.String)",
			                IdStringProvider.GetIdString(Lookup("int.Parse(string)")));
		}
		
		[Test]
		public void IntField()
		{
			Assert.AreEqual("Test.int",
			                Lookup("@int").ReflectionName);
		}
		
		[Test]
		public void ListOfT()
		{
			Assert.AreEqual("System.Collections.Generic.List`1",
			                Lookup("List{T}").ReflectionName);
		}
		
		[Test]
		public void ListOfTEnumerator()
		{
			Assert.AreEqual("System.Collections.Generic.List`1+Enumerator",
			                Lookup("List{T}.Enumerator").ReflectionName);
		}
		
		[Test]
		public void IDString()
		{
			Assert.AreEqual("System.Collections.Generic.List`1+Enumerator",
			                Lookup("T:System.Collections.Generic.List`1.Enumerator").ReflectionName);
		}
		
		[Test]
		public void M()
		{
			Assert.AreEqual("M:Test.M(System.Int32)",
			                IdStringProvider.GetIdString(Lookup("M")));
		}
		
		[Test]
		public void CurrentType()
		{
			Assert.AreEqual("T:Test",
			                IdStringProvider.GetIdString(Lookup("Test")));
		}
		
		[Test]
		public void Constructor()
		{
			Assert.AreEqual("M:Test.#ctor",
			                IdStringProvider.GetIdString(Lookup("Test()")));
		}
		
		[Test]
		public void Overloaded()
		{
			Assert.AreEqual("M:Test.Overloaded(System.Int32)",
			                IdStringProvider.GetIdString(Lookup("Overloaded(int)")));
			Assert.AreEqual("M:Test.Overloaded(System.String)",
			                IdStringProvider.GetIdString(Lookup("Overloaded(string)")));
			Assert.AreEqual("M:Test.Overloaded(System.Int32@)",
			                IdStringProvider.GetIdString(Lookup("Overloaded(ref int)")));
		}
		
		[Test]
		public void MethodInGenericInterface()
		{
			Assert.AreEqual("M:IGeneric`2.Test``1(``0[0:,0:]@)",
			                IdStringProvider.GetIdString(Lookup("IGeneric{X, Y}.Test")));
			Assert.AreEqual("M:IGeneric`2.Test``1(``0[0:,0:]@)",
			                IdStringProvider.GetIdString(Lookup("IGeneric{X, Y}.Test{Z}")));
			Assert.AreEqual("M:IGeneric`2.Test``1(``0[0:,0:]@)",
			                IdStringProvider.GetIdString(Lookup("IGeneric{X, Y}.Test{Z}(ref Z[,])")));
		}
		
		[Test]
		[Ignore("Fails due to mcs parser bug (see CSharpCrefParserTests.This)")]
		public void IndexerWithoutDeclaringType()
		{
			Assert.AreEqual("P:Test.Item(System.Int32)",
			                IdStringProvider.GetIdString(Lookup("this")));
		}
		
		[Test]
		public void IndexerWithDeclaringType()
		{
			Assert.AreEqual("P:Test.Item(System.Int32)",
			                IdStringProvider.GetIdString(Lookup("Test.this")));
			Assert.AreEqual("P:Test.Item(System.Int32)",
			                IdStringProvider.GetIdString(Lookup("Test.this[int]")));
		}
		
		[Test]
		[Ignore("mcs bug, see CSharpCrefParserTests.OperatorPlusWithDeclaringType")]
		public void OperatorPlus()
		{
			Assert.AreEqual("M:Test.op_Addition(Test,System.Int32)",
			                IdStringProvider.GetIdString(Lookup("operator +")));
			Assert.AreEqual("M:Test.op_Addition(Test,System.Int32)",
			                IdStringProvider.GetIdString(Lookup("operator +(Test, int)")));
			Assert.AreEqual("M:Test.op_Addition(Test,System.Int32)",
			                IdStringProvider.GetIdString(Lookup("Test.operator +(Test, int)")));
		}
		
		[Test]
		[Ignore("mcs bug, see CSharpCrefParserTests.OperatorPlusWithDeclaringType")]
		public void ImplicitOperator()
		{
			Assert.AreEqual("M:Test.op_Implicit(Test)~System.Int32",
			                IdStringProvider.GetIdString(Lookup("implicit operator int(Test)")));
			Assert.AreEqual("M:Test.op_Implicit(System.Int32)~Test",
			                IdStringProvider.GetIdString(Lookup("implicit operator Test(int)")));
			Assert.AreEqual("M:Test.op_Implicit(System.Int32)~Test",
			                IdStringProvider.GetIdString(Lookup("Test.implicit operator Test(int)")));
		}
	}
}
