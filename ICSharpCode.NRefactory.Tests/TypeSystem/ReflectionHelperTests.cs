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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.TypeSystem
{
	[TestFixture]
	public unsafe class ReflectionHelperTests
	{
		ITypeResolveContext context = CecilLoaderTests.Mscorlib;
		
		void TestGetClass(Type type)
		{
			ITypeDefinition t = CecilLoaderTests.Mscorlib.GetTypeDefinition(type);
			Assert.IsNotNull(t, type.FullName);
			Assert.AreEqual(type.FullName, t.ReflectionName);
		}
		
		[Test]
		public void TestGetInnerClass()
		{
			TestGetClass(typeof(Environment.SpecialFolder));
		}
		
		[Test]
		public void TestGetGenericClass1()
		{
			TestGetClass(typeof(Action<>));
		}
		
		[Test]
		public void TestGetGenericClass2()
		{
			TestGetClass(typeof(Action<,>));
		}
		
		[Test]
		public void TestGetInnerClassInGenericClass1()
		{
			TestGetClass(typeof(Dictionary<,>.ValueCollection));
		}
		
		[Test]
		public void TestGetInnerClassInGenericClass2()
		{
			TestGetClass(typeof(Dictionary<,>.ValueCollection.Enumerator));
		}
		
		[Test]
		public void TestToTypeReferenceInnerClass()
		{
			Assert.AreEqual("System.Environment+SpecialFolder",
			                typeof(Environment.SpecialFolder).ToTypeReference().Resolve(context).ReflectionName);
		}
		
		[Test]
		public void TestToTypeReferenceUnboundGenericClass()
		{
			Assert.AreEqual("System.Action`1",
			                typeof(Action<>).ToTypeReference().Resolve(context).ReflectionName);
			Assert.AreEqual("System.Action`2",
			                typeof(Action<,>).ToTypeReference().Resolve(context).ReflectionName);
		}
		
		[Test]
		public void TestToTypeReferenceBoundGenericClass()
		{
			Assert.AreEqual("System.Action`1[[System.String]]",
			                typeof(Action<string>).ToTypeReference().Resolve(context).ReflectionName);
			Assert.AreEqual("System.Action`2[[System.Int32],[System.Int16]]",
			                typeof(Action<int, short>).ToTypeReference().Resolve(context).ReflectionName);
		}
		
		
		[Test]
		public void TestToTypeReferenceNullableType()
		{
			Assert.AreEqual("System.Nullable`1[[System.Int32]]",
			                typeof(int?).ToTypeReference().Resolve(context).ReflectionName);
		}
		
		[Test]
		public void TestToTypeReferenceInnerClassInUnboundGenericType()
		{
			Assert.AreEqual("System.Collections.Generic.Dictionary`2+ValueCollection",
			                typeof(Dictionary<,>.ValueCollection).ToTypeReference().Resolve(context).ReflectionName);
		}
		
		[Test]
		public void TestToTypeReferenceInnerClassInBoundGenericType()
		{
			Assert.AreEqual("System.Collections.Generic.Dictionary`2+KeyCollection[[System.String],[System.Int32]]",
			                typeof(Dictionary<string, int>.KeyCollection).ToTypeReference().Resolve(context).ReflectionName);
		}
		
		[Test]
		public void TestToTypeReferenceArrayType()
		{
			Assert.AreEqual(typeof(int[]).FullName,
			                typeof(int[]).ToTypeReference().Resolve(context).ReflectionName);
		}
		
		[Test]
		public void TestToTypeReferenceMultidimensionalArrayType()
		{
			Assert.AreEqual(typeof(int[,]).FullName,
			                typeof(int[,]).ToTypeReference().Resolve(context).ReflectionName);
		}
		
		[Test]
		public void TestToTypeReferenceJaggedMultidimensionalArrayType()
		{
			Assert.AreEqual(typeof(int[,][,,]).FullName,
			                typeof(int[,][,,]).ToTypeReference().Resolve(context).ReflectionName);
		}
		
		[Test]
		public void TestToTypeReferencePointerType()
		{
			Assert.AreEqual(typeof(int*).FullName,
			                typeof(int*).ToTypeReference().Resolve(context).ReflectionName);
		}
		
		[Test]
		public void TestToTypeReferenceByReferenceType()
		{
			Assert.AreEqual(typeof(int).MakeByRefType().FullName,
			                typeof(int).MakeByRefType().ToTypeReference().Resolve(context).ReflectionName);
		}
		
		[Test]
		public void TestToTypeReferenceGenericType()
		{
			MethodInfo convertAllInfo = typeof(List<>).GetMethod("ConvertAll");
			Type parameterType = convertAllInfo.GetParameters()[0].ParameterType; // Converter[[`0],[``0]]
			// cannot resolve generic types without knowing the parent entity:
			Assert.AreEqual("System.Converter`2[[?],[?]]",
			                parameterType.ToTypeReference().Resolve(context).ReflectionName);
			// now try with parent entity:
			IMethod convertAll = context.GetTypeDefinition(typeof(List<>)).Methods.Single(m => m.Name == "ConvertAll");
			Assert.AreEqual("System.Converter`2[[`0],[``0]]",
			                parameterType.ToTypeReference(entity: convertAll).Resolve(context).ReflectionName);
		}
		
		[Test]
		public void ParseReflectionName()
		{
			Assert.AreEqual("System.Int32", ReflectionHelper.ParseReflectionName("System.Int32").Resolve(context).ReflectionName);
			Assert.AreEqual("System.Int32&", ReflectionHelper.ParseReflectionName("System.Int32&").Resolve(context).ReflectionName);
			Assert.AreEqual("System.Int32*&", ReflectionHelper.ParseReflectionName("System.Int32*&").Resolve(context).ReflectionName);
			Assert.AreEqual("System.Int32", ReflectionHelper.ParseReflectionName(typeof(int).AssemblyQualifiedName).Resolve(context).ReflectionName);
			Assert.AreEqual("System.Action`1[[System.String]]", ReflectionHelper.ParseReflectionName("System.Action`1[[System.String]]").Resolve(context).ReflectionName);
			Assert.AreEqual("System.Action`1[[System.String]]", ReflectionHelper.ParseReflectionName("System.Action`1[[System.String, mscorlib]]").Resolve(context).ReflectionName);
			Assert.AreEqual("System.Int32[,,][,]", ReflectionHelper.ParseReflectionName(typeof(int[,][,,]).AssemblyQualifiedName).Resolve(context).ReflectionName);
			Assert.AreEqual("System.Environment+SpecialFolder", ReflectionHelper.ParseReflectionName("System.Environment+SpecialFolder").Resolve(context).ReflectionName);
		}
		
		[Test]
		public void ParseOpenGenericReflectionName()
		{
			IMethod convertAll = context.GetTypeDefinition(typeof(List<>)).Methods.Single(m => m.Name == "ConvertAll");
			Assert.AreEqual("System.Converter`2[[?],[?]]", ReflectionHelper.ParseReflectionName("System.Converter`2[[`0],[``0]]").Resolve(context).ReflectionName);
			Assert.AreEqual("System.Converter`2[[`0],[``0]]", ReflectionHelper.ParseReflectionName("System.Converter`2[[`0],[``0]]", convertAll).Resolve(context).ReflectionName);
		}
		
		[Test, ExpectedException(typeof(ArgumentNullException))]
		public void ParseNullReflectionName()
		{
			ReflectionHelper.ParseReflectionName(null);
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName1()
		{
			ReflectionHelper.ParseReflectionName(string.Empty);
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName2()
		{
			ReflectionHelper.ParseReflectionName("`");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName3()
		{
			ReflectionHelper.ParseReflectionName("``");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName4()
		{
			ReflectionHelper.ParseReflectionName("System.Action`A");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName5()
		{
			ReflectionHelper.ParseReflectionName("System.Environment+");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName5b()
		{
			ReflectionHelper.ParseReflectionName("System.Environment+`");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName6()
		{
			ReflectionHelper.ParseReflectionName("System.Int32[");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName7()
		{
			ReflectionHelper.ParseReflectionName("System.Int32[`]");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName8()
		{
			ReflectionHelper.ParseReflectionName("System.Int32[,");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName9()
		{
			ReflectionHelper.ParseReflectionName("System.Int32]");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName10()
		{
			ReflectionHelper.ParseReflectionName("System.Int32*a");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName11()
		{
			ReflectionHelper.ParseReflectionName("System.Action`1[[]]");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName12()
		{
			ReflectionHelper.ParseReflectionName("System.Action`1[[System.Int32]a]");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName13()
		{
			ReflectionHelper.ParseReflectionName("System.Action`1[[System.Int32],]");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName14()
		{
			ReflectionHelper.ParseReflectionName("System.Action`1[[System.Int32]");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName15()
		{
			ReflectionHelper.ParseReflectionName("System.Action`1[[System.Int32");
		}
		
		[Test, ExpectedException(typeof(ReflectionNameParseException))]
		public void ParseInvalidReflectionName16()
		{
			ReflectionHelper.ParseReflectionName("System.Action`1[[System.Int32],[System.String");
		}
	}
}
