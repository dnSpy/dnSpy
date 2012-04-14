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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp
{
	[TestFixture]
	public class CSharpAmbienceTests
	{
		IUnresolvedAssembly mscorlib;
		IUnresolvedAssembly myLib;
		ICompilation compilation;
		CSharpAmbience ambience;
		
		public CSharpAmbienceTests()
		{
			ambience = new CSharpAmbience();
			mscorlib = CecilLoaderTests.Mscorlib;
			var loader = new CecilLoader();
			loader.IncludeInternalMembers = true;
			myLib = loader.LoadAssemblyFile(typeof(CSharpAmbienceTests).Assembly.Location);
			compilation = new SimpleCompilation(myLib, mscorlib);
		}
		
		#region ITypeDefinition tests
		[Test]
		public void GenericType()
		{
			var typeDef = compilation.FindType(typeof(Dictionary<,>)).GetDefinition();
			ambience.ConversionFlags = ConversionFlags.UseFullyQualifiedTypeNames | ConversionFlags.ShowTypeParameterList;
			string result = ambience.ConvertEntity(typeDef);
			
			Assert.AreEqual("System.Collections.Generic.Dictionary<TKey, TValue>", result);
		}
		
		[Test]
		public void GenericTypeShortName()
		{
			var typeDef = compilation.FindType(typeof(Dictionary<,>)).GetDefinition();
			ambience.ConversionFlags = ConversionFlags.ShowTypeParameterList;
			string result = ambience.ConvertEntity(typeDef);
			
			Assert.AreEqual("Dictionary<TKey, TValue>", result);
		}
		
		[Test]
		public void SimpleType()
		{
			var typeDef = compilation.FindType(typeof(Object)).GetDefinition();
			ambience.ConversionFlags = ConversionFlags.UseFullyQualifiedTypeNames | ConversionFlags.ShowTypeParameterList;
			string result = ambience.ConvertEntity(typeDef);
			
			Assert.AreEqual("System.Object", result);
		}
		
		[Test]
		public void SimpleTypeDefinition()
		{
			var typeDef = compilation.FindType(typeof(Object)).GetDefinition();
			ambience.ConversionFlags = ConversionFlags.All & ~(ConversionFlags.UseFullyQualifiedTypeNames);
			string result = ambience.ConvertEntity(typeDef);
			
			Assert.AreEqual("public class Object", result);
		}
		
		[Test]
		public void SimpleTypeDefinitionWithoutModifiers()
		{
			var typeDef = compilation.FindType(typeof(Object)).GetDefinition();
			ambience.ConversionFlags = ConversionFlags.All & ~(ConversionFlags.UseFullyQualifiedTypeNames | ConversionFlags.ShowModifiers | ConversionFlags.ShowAccessibility);
			string result = ambience.ConvertEntity(typeDef);
			
			Assert.AreEqual("class Object", result);
		}
		
		[Test]
		public void GenericTypeDefinitionFull()
		{
			var typeDef = compilation.FindType(typeof(List<>)).GetDefinition();
			ambience.ConversionFlags = ConversionFlags.All;
			string result = ambience.ConvertEntity(typeDef);
			
			Assert.AreEqual("public class System.Collections.Generic.List<T>", result);
		}
		
		[Test]
		public void GenericInterfaceFull()
		{
			var typeDef = compilation.FindType(typeof(IEnumerable<>)).GetDefinition();
			ambience.ConversionFlags = ConversionFlags.All;
			string result = ambience.ConvertEntity(typeDef);
			
			Assert.AreEqual("public interface System.Collections.Generic.IEnumerable<out T>", result);
		}
		
		[Test]
		public void SimpleTypeShortName()
		{
			var typeDef = compilation.FindType(typeof(Object)).GetDefinition();
			ambience.ConversionFlags = ConversionFlags.ShowTypeParameterList;
			string result = ambience.ConvertEntity(typeDef);
			
			Assert.AreEqual("Object", result);
		}
		
		[Test]
		public void GenericTypeWithNested()
		{
			var typeDef = compilation.FindType(typeof(List<>.Enumerator)).GetDefinition();
			ambience.ConversionFlags = ConversionFlags.UseFullyQualifiedTypeNames | ConversionFlags.ShowTypeParameterList;
			string result = ambience.ConvertEntity(typeDef);
			
			Assert.AreEqual("System.Collections.Generic.List<T>.Enumerator", result);
		}
		
		[Test]
		public void GenericTypeWithNestedShortName()
		{
			var typeDef = compilation.FindType(typeof(List<>.Enumerator)).GetDefinition();
			ambience.ConversionFlags = ConversionFlags.ShowDeclaringType | ConversionFlags.ShowTypeParameterList;
			string result = ambience.ConvertEntity(typeDef);
			
			Assert.AreEqual("List<T>.Enumerator", result);
		}
		#endregion
		
		#region Delegate tests
		[Test]
		public void DelegateName()
		{
			var func = compilation.FindType(typeof(Func<,>)).GetDefinition();
			ambience.ConversionFlags = ConversionFlags.ShowTypeParameterList;
			
			Assert.AreEqual("Func<in T, out TResult>", ambience.ConvertEntity(func));
		}
		
		[Test]
		public void FullDelegate()
		{
			var func = compilation.FindType(typeof(Func<,>)).GetDefinition();
			ambience.ConversionFlags = ConversionFlags.All;
			Assert.AreEqual("public delegate TResult System.Func<in T, out TResult>(T arg);", ambience.ConvertEntity(func));
		}
		#endregion
		
		#region IField tests
		[Test]
		public void SimpleField()
		{
			var field = compilation.FindType(typeof(CSharpAmbienceTests.Program)).GetFields(f => f.Name == "test").Single();
			ambience.ConversionFlags = ConversionFlags.All;
			string result = ambience.ConvertEntity(field);
			
			Assert.AreEqual("private int ICSharpCode.NRefactory.CSharp.CSharpAmbienceTests.Program.test;", result);
		}
		
		[Test]
		public void SimpleConstField()
		{
			var field = compilation.FindType(typeof(CSharpAmbienceTests.Program)).GetFields(f => f.Name == "TEST2").Single();
			ambience.ConversionFlags = ConversionFlags.All;
			string result = ambience.ConvertEntity(field);
			
			Assert.AreEqual("private const int ICSharpCode.NRefactory.CSharp.CSharpAmbienceTests.Program.TEST2;", result);
		}
		
		[Test]
		public void SimpleFieldWithoutModifiers()
		{
			var field = compilation.FindType(typeof(CSharpAmbienceTests.Program)).GetFields(f => f.Name == "test").Single();
			ambience.ConversionFlags = ConversionFlags.All & ~(ConversionFlags.ShowDeclaringType | ConversionFlags.ShowModifiers | ConversionFlags.ShowAccessibility);
			string result = ambience.ConvertEntity(field);
			
			Assert.AreEqual("int test;", result);
		}
		#endregion
		
		#region IEvent tests
		[Test]
		public void EventWithDeclaringType()
		{
			var ev = compilation.FindType(typeof(CSharpAmbienceTests.Program)).GetEvents(f => f.Name == "ProgramChanged").Single();
			ambience.ConversionFlags = ConversionFlags.StandardConversionFlags | ConversionFlags.ShowDeclaringType;
			string result = ambience.ConvertEntity(ev);
			
			Assert.AreEqual("public event EventHandler Program.ProgramChanged;", result);
		}
		
		[Test]
		public void CustomEvent()
		{
			var ev = compilation.FindType(typeof(CSharpAmbienceTests.Program)).GetEvents(f => f.Name == "SomeEvent").Single();
			ambience.ConversionFlags = ConversionFlags.StandardConversionFlags;
			string result = ambience.ConvertEntity(ev);
			
			Assert.AreEqual("public event EventHandler SomeEvent;", result);
		}
		#endregion
		
		#region Property tests
		[Test]
		public void AutomaticProperty()
		{
			var prop = compilation.FindType(typeof(CSharpAmbienceTests.Program)).GetProperties(p => p.Name == "Test").Single();
			ambience.ConversionFlags = ConversionFlags.StandardConversionFlags;
			string result = ambience.ConvertEntity(prop);
			
			Assert.AreEqual("public int Test { get; set; }", result);
		}
		
		[Test]
		public void Indexer()
		{
			var prop = compilation.FindType(typeof(CSharpAmbienceTests.Program)).GetProperties(p => p.IsIndexer).Single();
			ambience.ConversionFlags = ConversionFlags.StandardConversionFlags;
			string result = ambience.ConvertEntity(prop);
			
			Assert.AreEqual("public int this[int index] { get; }", result);
		}
		#endregion
		
		#region Test types
		#pragma warning disable 169, 67
		
		class Test {}
		
		class Program
		{
			int test;
			const int TEST2 = 2;
			
			public int Test { get; set; }
			
			public int this[int index] {
				get {
					return index;
				}
			}
			
			public event EventHandler ProgramChanged;
			
			public event EventHandler SomeEvent {
				add {}
				remove {}
			}
			
			public static bool operator +(Program lhs, Program rhs)
			{
				throw new NotImplementedException();
			}
			
			public static implicit operator Test(Program lhs)
			{
				throw new NotImplementedException();
			}
			
			public static explicit operator int(Program lhs)
			{
				throw new NotImplementedException();
			}
			
			public Program(int x)
			{
				
			}
			
			~Program()
			{
				
			}
			
			public static void Main(string[] args)
			{
				Console.WriteLine("Hello World!");
				
				Console.Write("Press any key to continue . . . ");
				Console.ReadKey(true);
			}
		}
		#endregion
	}
}
