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
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.Documentation
{
	[TestFixture]
	public class IDStringTests
	{
		class IDStringTestProjectContent : SimpleProjectContent, IDocumentationProvider
		{
			public string GetDocumentation(IEntity entity)
			{
				// Note: there's no mscorlib in the context.
				// These tests only use primitive types from mscorlib, so the full name is known
				// without resolving them.
				return IDStringProvider.GetIDString(entity, this);
			}
		}
		
		IDStringTestProjectContent pc;
		
		void Init(string program)
		{
			pc = new IDStringTestProjectContent();
			
			var cu = new CSharpParser().Parse(new StringReader(program));
			var parsedFile = new TypeSystemConvertVisitor(pc, "program.cs").Convert(cu);
			pc.UpdateProjectContent(null, parsedFile);
		}
		
		ITypeDefinition GetTypeDefinition(string nameSpace, string name, int typeParameterCount = 0)
		{
			return pc.GetTypeDefinition(nameSpace, name, typeParameterCount, StringComparer.Ordinal);
		}
		
		[Test]
		public void TypeDefinitions()
		{
			string program = @"
enum Color { Red, Blue, Green }
namespace Acme
{
	interface IProcess {}
	struct ValueType {}
	class Widget: IProcess
	{
		public class NestedClass {}
		
		public interface IMenuItem {}
		
		public delegate void Del(int i);
		
		public enum Direction { North, South, East, West }
	}
	class MyList<T>
	{
		class Helper<U,V> {}
	}
}";
			Init(program);
			Assert.AreEqual("T:Color", GetTypeDefinition(string.Empty, "Color").Documentation);
			Assert.AreEqual("T:Acme.IProcess", GetTypeDefinition("Acme", "IProcess").Documentation);
			Assert.AreEqual("T:Acme.ValueType", GetTypeDefinition("Acme", "ValueType").Documentation);
			
			ITypeDefinition widget = GetTypeDefinition("Acme", "Widget");
			Assert.AreEqual("T:Acme.Widget", widget.Documentation);
			Assert.AreEqual("T:Acme.Widget.NestedClass", widget.NestedTypes.Single(t => t.Name == "NestedClass").Documentation);
			Assert.AreEqual("T:Acme.Widget.IMenuItem", widget.NestedTypes.Single(t => t.Name == "IMenuItem").Documentation);
			Assert.AreEqual("T:Acme.Widget.Del", widget.NestedTypes.Single(t => t.Name == "Del").Documentation);
			Assert.AreEqual("T:Acme.Widget.Direction", widget.NestedTypes.Single(t => t.Name == "Direction").Documentation);
			Assert.AreEqual("T:Acme.MyList`1", GetTypeDefinition("Acme", "MyList", 1).Documentation);
			Assert.AreEqual("T:Acme.MyList`1.Helper`2", GetTypeDefinition("Acme", "MyList", 1).NestedTypes.Single().Documentation);
		}
		
		[Test]
		public void Fields()
		{
			string program = @"
namespace Acme
{
	class Widget : IProcess
	{
		public class NestedClass
		{
			private int value;
		}
		
		private string message;
		private const double PI = 3.14159;
		private unsafe float **ppValues;
	}
}";
			Init(program);
			ITypeDefinition widget = GetTypeDefinition("Acme", "Widget");
			Assert.AreEqual("F:Acme.Widget.NestedClass.value", widget.NestedTypes.Single().Fields.Single().Documentation);
			Assert.AreEqual("F:Acme.Widget.message", widget.Fields.Single(f => f.Name == "message").Documentation);
			Assert.AreEqual("F:Acme.Widget.PI", widget.Fields.Single(f => f.Name == "PI").Documentation);
			Assert.AreEqual("F:Acme.Widget.ppValues", widget.Fields.Single(f => f.Name == "ppValues").Documentation);
		}
		
		[Test]
		public void Constructors()
		{
			string program = @"
namespace Acme
{
	class Widget: IProcess
	{
		static Widget() {}
		public Widget() {}
		public Widget(string s) {}
	}
}";
			Init(program);
			ITypeDefinition widget = GetTypeDefinition("Acme", "Widget");
			Assert.AreEqual("M:Acme.Widget.#cctor", widget.Methods.Single(m => m.IsStatic).Documentation);
			Assert.AreEqual("M:Acme.Widget.#ctor", widget.Methods.Single(m => !m.IsStatic && m.Parameters.Count == 0).Documentation);
			Assert.AreEqual("M:Acme.Widget.#ctor(System.String)", widget.Methods.Single(m => m.Parameters.Count == 1).Documentation);
		}
		
		[Test]
		public void Destructor()
		{
			string program = @"
namespace Acme
{
	class Widget: IProcess
	{
		~Widget() { }
	}
}";
			Init(program);
			ITypeDefinition widget = GetTypeDefinition("Acme", "Widget");
			Assert.AreEqual("M:Acme.Widget.Finalize", widget.Methods.Single().Documentation);
		}
		
		[Test]
		public void Methods()
		{
			string program = @"
enum Color {}
namespace Acme
{
	struct ValueType { }
	class Widget: IProcess
	{
		public class NestedClass
		{
			public void M(int i) {}
		}
		
		public static void M0() {...}
		public void M1(char c, out float f, ref ValueType v) {...}
		public void M2(short[] x1, int[,] x2, long[][] x3) {...}
		public void M3(long[][] x3, Widget[][,,] x4) {...}
		public unsafe void M4(char *pc, Color **pf) {...}
		public unsafe void M5(void *pv, double *[][,] pd) {...}
		public void M6(int? i, params object[] args) {...}
	}
	
	class MyList<T>
	{
		public void Test(T t) { }
	}
	
	class UseList
	{
		public void Process(MyList<Color> list) { }
		public MyList<T> GetValues<T>(T inputValue) { return null; }
	}
}";
			Init(program);
			ITypeDefinition widget = GetTypeDefinition("Acme", "Widget");
			Assert.AreEqual("M:Acme.Widget.NestedClass.M(System.Int32)", widget.NestedTypes.Single().Methods.Single().Documentation);
			Assert.AreEqual("M:Acme.Widget.M0", widget.Methods.Single(m => m.Name == "M0").Documentation);
			Assert.AreEqual("M:Acme.Widget.M1(System.Char,System.Single@,Acme.ValueType@)",
			                widget.Methods.Single(m => m.Name == "M1").Documentation);
			Assert.AreEqual("M:Acme.Widget.M2(System.Int16[],System.Int32[0:,0:],System.Int64[][])",
			                widget.Methods.Single(m => m.Name == "M2").Documentation);
			Assert.AreEqual("M:Acme.Widget.M3(System.Int64[][],Acme.Widget[0:,0:,0:][])",
			                widget.Methods.Single(m => m.Name == "M3").Documentation);
			Assert.AreEqual("M:Acme.Widget.M4(System.Char*,Color**)",
			                widget.Methods.Single(m => m.Name == "M4").Documentation);
			Assert.AreEqual("M:Acme.Widget.M5(System.Void*,System.Double*[0:,0:][])",
			                widget.Methods.Single(m => m.Name == "M5").Documentation);
			Assert.AreEqual("M:Acme.Widget.M6(System.Nullable{System.Int32},System.Object[])",
			                widget.Methods.Single(m => m.Name == "M6").Documentation);
			
			Assert.AreEqual("M:Acme.MyList`1.Test(`0)",
			                GetTypeDefinition("Acme", "MyList", 1).Methods.Single().Documentation);
			
			Assert.AreEqual("M:Acme.UseList.Process(Acme.MyList{Color})",
			                GetTypeDefinition("Acme", "UseList").Methods.Single(m => m.Name == "Process").Documentation);
			Assert.AreEqual("M:Acme.UseList.GetValues``1(``0)",
			                GetTypeDefinition("Acme", "UseList").Methods.Single(m => m.Name == "GetValues").Documentation);
		}
		
		[Test]
		public void NestedGenerics()
		{
			Init("class A<X> { class B<Y> { void M(A<Y>.B<X> a) { } } }");
			ITypeDefinition b = GetTypeDefinition("", "A", 1).NestedTypes.Single();
			Assert.AreEqual("T:A`1.B`1", b.Documentation);
			Assert.AreEqual("M:A`1.B`1.M(A{`1}.B{`0})", b.Methods.Single().Documentation);
		}
		
		[Test]
		public void Properties()
		{
			string program = @"
namespace Acme
{
	class Widget: IProcess
	{
		public int Width { get {...} set {...} }
		public int this[int i] { get {...} set {...} }
		public int this[string s, int i] { get {...} set {...} }
	}
}";
			Init(program);
			ITypeDefinition widget = GetTypeDefinition("Acme", "Widget");
			Assert.AreEqual("P:Acme.Widget.Width", widget.Properties.Single(p => p.Parameters.Count == 0).Documentation);
			Assert.AreEqual("P:Acme.Widget.Item(System.Int32)",
			                widget.Properties.Single(p => p.Parameters.Count == 1).Documentation);
			Assert.AreEqual("P:Acme.Widget.Item(System.String,System.Int32)",
			                widget.Properties.Single(p => p.Parameters.Count == 2).Documentation);
			
		}
		
		[Test]
		public void Event()
		{
			string program = @"
namespace Acme
{
	class Widget: IProcess
	{
		public event Del AnEvent;
	}
}";
			Init(program);
			ITypeDefinition widget = GetTypeDefinition("Acme", "Widget");
			Assert.AreEqual("E:Acme.Widget.AnEvent", widget.Events.Single().Documentation);
			
		}
		
		[Test]
		public void UnaryOperator()
		{
			string program = @"
namespace Acme
{
	class Widget: IProcess
	{
		public static Widget operator+(Widget x) { }
	}
}";
			Init(program);
			ITypeDefinition widget = GetTypeDefinition("Acme", "Widget");
			Assert.AreEqual("M:Acme.Widget.op_UnaryPlus(Acme.Widget)", widget.Methods.Single().Documentation);
		}
		
		[Test]
		public void BinaryOperator()
		{
			string program = @"
namespace Acme
{
	class Widget: IProcess
	{
		public static Widget operator+(Widget x1, Widget x2) { }
	}
}";
			Init(program);
			ITypeDefinition widget = GetTypeDefinition("Acme", "Widget");
			Assert.AreEqual("M:Acme.Widget.op_Addition(Acme.Widget,Acme.Widget)", widget.Methods.Single().Documentation);
		}
		
		[Test]
		public void ConversionOperator()
		{
			string program = @"
namespace Acme
{
	class Widget: IProcess
	{
		public static explicit operator int(Widget x) { }
		public static implicit operator long(Widget x) { }
	}
}";
			Init(program);
			ITypeDefinition widget = GetTypeDefinition("Acme", "Widget");
			Assert.AreEqual("M:Acme.Widget.op_Explicit(Acme.Widget)~System.Int32", widget.Methods.First().Documentation);
			Assert.AreEqual("M:Acme.Widget.op_Implicit(Acme.Widget)~System.Int64", widget.Methods.Last().Documentation);
		}
		
		[Test]
		public void CorlibIDStrings()
		{
			var list = CecilLoaderTests.Mscorlib.GetTypeDefinition(typeof(List<>));
			Assert.AreEqual("T:System.Collections.Generic.List`1",
			                IDStringProvider.GetIDString(list));
			Assert.AreEqual("M:System.Collections.Generic.List`1.Add(`0)",
			                IDStringProvider.GetIDString(list.Methods.Single(m => m.Name == "Add")));
			Assert.AreEqual("M:System.Collections.Generic.List`1.AddRange(System.Collections.Generic.IEnumerable{`0})",
			                IDStringProvider.GetIDString(list.Methods.Single(m => m.Name == "AddRange")));
			Assert.AreEqual("M:System.Collections.Generic.List`1.ConvertAll``1(System.Converter{`0,``0})",
			                IDStringProvider.GetIDString(list.Methods.Single(m => m.Name == "ConvertAll")));
		}
	}
}
