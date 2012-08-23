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
using System.Text;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.TypeSystem
{
	[TestFixture]
	public class CecilLoaderTests : TypeSystemTests
	{
		static readonly Lazy<IUnresolvedAssembly> mscorlib = new Lazy<IUnresolvedAssembly>(
			delegate {
				return new CecilLoader().LoadAssemblyFile(typeof(object).Assembly.Location);
			});
		
		static readonly Lazy<IUnresolvedAssembly> systemCore = new Lazy<IUnresolvedAssembly>(
			delegate {
			return new CecilLoader().LoadAssemblyFile(typeof(System.Linq.Enumerable).Assembly.Location);
		});

		public static IUnresolvedAssembly Mscorlib { get { return mscorlib.Value; } }
		public static IUnresolvedAssembly SystemCore { get { return systemCore.Value; } }

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			// use "IncludeInternalMembers" so that Cecil results match C# parser results
			CecilLoader loader = new CecilLoader() { IncludeInternalMembers = true };
			IUnresolvedAssembly asm = loader.LoadAssemblyFile(typeof(TestCase.SimplePublicClass).Assembly.Location);
			compilation = new SimpleCompilation(asm, CecilLoaderTests.Mscorlib);
		}
		
		[Test]
		public void InheritanceTest()
		{
			ITypeDefinition c = compilation.FindType(typeof(SystemException)).GetDefinition();
			ITypeDefinition c2 = compilation.FindType(typeof(Exception)).GetDefinition();
			Assert.IsNotNull(c, "c is null");
			Assert.IsNotNull(c2, "c2 is null");
			//Assert.AreEqual(3, c.BaseTypes.Count); // Inherited interfaces are not reported by Cecil
			// which matches the behaviour of our C#/VB parsers
			Assert.AreEqual("System.Exception", c.DirectBaseTypes.First().FullName);
			Assert.AreSame(c2, c.DirectBaseTypes.First());
			
			string[] superTypes = c.GetAllBaseTypes().Select(t => t.ReflectionName).ToArray();
			Assert.AreEqual(new string[] {
			                	"System.Object",
			                	"System.Runtime.Serialization.ISerializable", "System.Runtime.InteropServices._Exception",
			                	"System.Exception", "System.SystemException"
			                }, superTypes);
		}
		
		[Test]
		public void GenericPropertyTest()
		{
			ITypeDefinition c = compilation.FindType(typeof(Comparer<>)).GetDefinition();
			IProperty def = c.Properties.Single(p => p.Name == "Default");
			ParameterizedType pt = (ParameterizedType)def.ReturnType;
			Assert.AreEqual("System.Collections.Generic.Comparer", pt.FullName);
			Assert.AreEqual(c.TypeParameters[0], pt.TypeArguments[0]);
		}
		
		[Test]
		public void PointerTypeTest()
		{
			ITypeDefinition c = compilation.FindType(typeof(IntPtr)).GetDefinition();
			IMethod toPointer = c.Methods.Single(p => p.Name == "ToPointer");
			Assert.AreEqual("System.Void*", toPointer.ReturnType.ReflectionName);
			Assert.IsTrue (toPointer.ReturnType is PointerType);
			Assert.AreEqual("System.Void", ((PointerType)toPointer.ReturnType).ElementType.FullName);
		}
		
		[Test]
		public void DateTimeDefaultConstructor()
		{
			ITypeDefinition c = compilation.FindType(typeof(DateTime)).GetDefinition();
			Assert.AreEqual(1, c.Methods.Count(m => m.IsConstructor && m.Parameters.Count == 0));
			Assert.AreEqual(1, c.GetConstructors().Count(m => m.Parameters.Count == 0));
		}
		
		[Test]
		public void NoEncodingInfoDefaultConstructor()
		{
			ITypeDefinition c = compilation.FindType(typeof(EncodingInfo)).GetDefinition();
			// EncodingInfo only has an internal constructor
			Assert.IsFalse(c.Methods.Any(m => m.IsConstructor));
			// and no implicit ctor should be added:
			Assert.AreEqual(0, c.GetConstructors().Count());
		}
		
		[Test]
		public void StaticModifierTest()
		{
			ITypeDefinition c = compilation.FindType(typeof(Environment)).GetDefinition();
			Assert.IsNotNull(c, "System.Environment not found");
			Assert.IsTrue(c.IsAbstract, "class should be abstract");
			Assert.IsTrue(c.IsSealed, "class should be sealed");
			Assert.IsTrue(c.IsStatic, "class should be static");
		}
		
		[Test]
		public void InnerClassReferenceTest()
		{
			ITypeDefinition c = compilation.FindType(typeof(Environment)).GetDefinition();
			Assert.IsNotNull(c, "System.Environment not found");
			IType rt = c.Methods.First(m => m.Name == "GetFolderPath").Parameters[0].Type;
			Assert.AreSame(c.NestedTypes.Single(ic => ic.Name == "SpecialFolder"), rt);
		}
		
		[Test]
		public void NestedTypesTest()
		{
			ITypeDefinition c = compilation.FindType(typeof(Environment.SpecialFolder)).GetDefinition();
			Assert.IsNotNull(c, "c is null");
			Assert.AreEqual("System.Environment.SpecialFolder", c.FullName);
			Assert.AreEqual("System.Environment+SpecialFolder", c.ReflectionName);
		}
		
		[Test]
		public void VoidHasNoMembers()
		{
			ITypeDefinition c = compilation.FindType(typeof(void)).GetDefinition();
			Assert.IsNotNull(c, "System.Void not found");
			Assert.AreEqual(0, c.GetMethods().Count());
			Assert.AreEqual(0, c.GetProperties().Count());
			Assert.AreEqual(0, c.GetEvents().Count());
			Assert.AreEqual(0, c.GetFields().Count());
		}
		
		[Test]
		public void Void_SerializableAttribute()
		{
			ITypeDefinition c = compilation.FindType(typeof(void)).GetDefinition();
			var attr = c.Attributes.Single(a => a.AttributeType.FullName == "System.SerializableAttribute");
			Assert.AreEqual(0, attr.Constructor.Parameters.Count);
			Assert.AreEqual(0, attr.PositionalArguments.Count);
			Assert.AreEqual(0, attr.NamedArguments.Count);
		}
		
		[Test]
		public void Void_StructLayoutAttribute()
		{
			ITypeDefinition c = compilation.FindType(typeof(void)).GetDefinition();
			var attr = c.Attributes.Single(a => a.AttributeType.FullName == "System.Runtime.InteropServices.StructLayoutAttribute");
			Assert.AreEqual(1, attr.Constructor.Parameters.Count);
			Assert.AreEqual(1, attr.PositionalArguments.Count);
			Assert.AreEqual(0, attr.PositionalArguments[0].ConstantValue);
			Assert.AreEqual(1, attr.NamedArguments.Count);
			Assert.AreEqual("System.Runtime.InteropServices.StructLayoutAttribute.Size", attr.NamedArguments[0].Key.FullName);
			Assert.AreEqual(1, attr.NamedArguments[0].Value.ConstantValue);
		}
		
		[Test]
		public void Void_ComVisibleAttribute()
		{
			ITypeDefinition c = compilation.FindType(typeof(void)).GetDefinition();
			var attr = c.Attributes.Single(a => a.AttributeType.FullName == "System.Runtime.InteropServices.ComVisibleAttribute");
			Assert.AreEqual(1, attr.Constructor.Parameters.Count);
			Assert.AreEqual(1, attr.PositionalArguments.Count);
			Assert.AreEqual(true, attr.PositionalArguments[0].ConstantValue);
			Assert.AreEqual(0, attr.NamedArguments.Count);
		}
		
		[Test]
		public void NestedClassInGenericClassTest()
		{
			ITypeDefinition dictionary = compilation.FindType(typeof(Dictionary<,>)).GetDefinition();
			Assert.IsNotNull(dictionary);
			ITypeDefinition valueCollection = compilation.FindType(typeof(Dictionary<,>.ValueCollection)).GetDefinition();
			Assert.IsNotNull(valueCollection);
			var dictionaryRT = new ParameterizedType(dictionary, new[] { compilation.FindType(typeof(string)).GetDefinition(), compilation.FindType(typeof(int)).GetDefinition() });
			IProperty valueProperty = dictionaryRT.GetProperties(p => p.Name == "Values").Single();
			IType parameterizedValueCollection = valueProperty.ReturnType;
			Assert.AreEqual("System.Collections.Generic.Dictionary`2+ValueCollection[[System.String],[System.Int32]]", parameterizedValueCollection.ReflectionName);
			Assert.AreSame(valueCollection, parameterizedValueCollection.GetDefinition());
		}
		
		[Test]
		public void ValueCollectionCountModifiers()
		{
			ITypeDefinition valueCollection = compilation.FindType(typeof(Dictionary<,>.ValueCollection)).GetDefinition();
			Assert.AreEqual(Accessibility.Public, valueCollection.Accessibility);
			Assert.IsTrue(valueCollection.IsSealed);
			Assert.IsFalse(valueCollection.IsAbstract);
			Assert.IsFalse(valueCollection.IsStatic);
			
			IProperty count = valueCollection.Properties.Single(p => p.Name == "Count");
			Assert.AreEqual(Accessibility.Public, count.Accessibility);
			// It's sealed on the IL level; but in C# it's just a normal non-virtual method that happens to implement an interface
			Assert.IsFalse(count.IsSealed);
			Assert.IsFalse(count.IsVirtual);
			Assert.IsFalse(count.IsAbstract);
		}
		
		[Test]
		public void MathAcosModifiers()
		{
			ITypeDefinition math = compilation.FindType(typeof(Math)).GetDefinition();
			Assert.AreEqual(Accessibility.Public, math.Accessibility);
			Assert.IsTrue(math.IsSealed);
			Assert.IsTrue(math.IsAbstract);
			Assert.IsTrue(math.IsStatic);
			
			IMethod acos = math.Methods.Single(p => p.Name == "Acos");
			Assert.AreEqual(Accessibility.Public, acos.Accessibility);
			Assert.IsTrue(acos.IsStatic);
			Assert.IsFalse(acos.IsAbstract);
			Assert.IsFalse(acos.IsSealed);
			Assert.IsFalse(acos.IsVirtual);
			Assert.IsFalse(acos.IsOverride);
		}
		
		[Test]
		public void EncodingModifiers()
		{
			ITypeDefinition encoding = compilation.FindType(typeof(Encoding)).GetDefinition();
			Assert.AreEqual(Accessibility.Public, encoding.Accessibility);
			Assert.IsFalse(encoding.IsSealed);
			Assert.IsTrue(encoding.IsAbstract);
			
			IMethod getDecoder = encoding.Methods.Single(p => p.Name == "GetDecoder");
			Assert.AreEqual(Accessibility.Public, getDecoder.Accessibility);
			Assert.IsFalse(getDecoder.IsStatic);
			Assert.IsFalse(getDecoder.IsAbstract);
			Assert.IsFalse(getDecoder.IsSealed);
			Assert.IsTrue(getDecoder.IsVirtual);
			Assert.IsFalse(getDecoder.IsOverride);
			
			IMethod getMaxByteCount = encoding.Methods.Single(p => p.Name == "GetMaxByteCount");
			Assert.AreEqual(Accessibility.Public, getMaxByteCount.Accessibility);
			Assert.IsFalse(getMaxByteCount.IsStatic);
			Assert.IsTrue(getMaxByteCount.IsAbstract);
			Assert.IsFalse(getMaxByteCount.IsSealed);
			Assert.IsFalse(getMaxByteCount.IsVirtual);
			Assert.IsFalse(getMaxByteCount.IsOverride);
			
			IProperty encoderFallback = encoding.Properties.Single(p => p.Name == "EncoderFallback");
			Assert.AreEqual(Accessibility.Public, encoderFallback.Accessibility);
			Assert.IsFalse(encoderFallback.IsStatic);
			Assert.IsFalse(encoderFallback.IsAbstract);
			Assert.IsFalse(encoderFallback.IsSealed);
			Assert.IsFalse(encoderFallback.IsVirtual);
			Assert.IsFalse(encoderFallback.IsOverride);
		}
		
		[Test]
		public void UnicodeEncodingModifiers()
		{
			ITypeDefinition encoding = compilation.FindType(typeof(UnicodeEncoding)).GetDefinition();
			Assert.AreEqual(Accessibility.Public, encoding.Accessibility);
			Assert.IsFalse(encoding.IsSealed);
			Assert.IsFalse(encoding.IsAbstract);
			
			IMethod getDecoder = encoding.Methods.Single(p => p.Name == "GetDecoder");
			Assert.AreEqual(Accessibility.Public, getDecoder.Accessibility);
			Assert.IsFalse(getDecoder.IsStatic);
			Assert.IsFalse(getDecoder.IsAbstract);
			Assert.IsFalse(getDecoder.IsSealed);
			Assert.IsFalse(getDecoder.IsVirtual);
			Assert.IsTrue(getDecoder.IsOverride);
		}
		
		[Test]
		public void UTF32EncodingModifiers()
		{
			ITypeDefinition encoding = compilation.FindType(typeof(UTF32Encoding)).GetDefinition();
			Assert.AreEqual(Accessibility.Public, encoding.Accessibility);
			Assert.IsTrue(encoding.IsSealed);
			Assert.IsFalse(encoding.IsAbstract);
			
			IMethod getDecoder = encoding.Methods.Single(p => p.Name == "GetDecoder");
			Assert.AreEqual(Accessibility.Public, getDecoder.Accessibility);
			Assert.IsFalse(getDecoder.IsStatic);
			Assert.IsFalse(getDecoder.IsAbstract);
			Assert.IsFalse(getDecoder.IsSealed);
			Assert.IsFalse(getDecoder.IsVirtual);
			Assert.IsTrue(getDecoder.IsOverride);
		}
		
		[Test]
		public void FindRedirectedType()
		{
			var compilationWithSystemCore = new SimpleCompilation(systemCore.Value, mscorlib.Value);
			
			var typeRef = ReflectionHelper.ParseReflectionName("System.Func`2, System.Core");
			ITypeDefinition c = typeRef.Resolve(compilationWithSystemCore.TypeResolveContext).GetDefinition();
			Assert.IsNotNull(c, "System.Func<,> not found");
			Assert.AreEqual("mscorlib", c.ParentAssembly.AssemblyName);
		}
		
		public void DelegateIsClass()
		{
			var @delegate = compilation.FindType(KnownTypeCode.Delegate).GetDefinition();
			Assert.AreEqual(TypeKind.Class, @delegate);
			Assert.IsFalse(@delegate.IsSealed);
		}
		
		public void MulticastDelegateIsClass()
		{
			var multicastDelegate = compilation.FindType(KnownTypeCode.MulticastDelegate).GetDefinition();
			Assert.AreEqual(TypeKind.Class, multicastDelegate);
			Assert.IsFalse(multicastDelegate.IsSealed);
		}
	}
}
