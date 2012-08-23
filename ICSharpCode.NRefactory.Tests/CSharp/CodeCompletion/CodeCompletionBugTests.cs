//
// CodeCompletionBugTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeCompletion
{
	[TestFixture()]
	public class CodeCompletionBugTests : TestBase
	{

		public static CompletionDataList CreateProvider (string text)
		{
			return CreateProvider (text, false);
		}
		
		public static CompletionDataList CreateCtrlSpaceProvider (string text)
		{
			return CreateProvider (text, true);
		}
		
		public static void CombinedProviderTest (string text, Action<CompletionDataList> act)
		{
			var provider = CreateProvider (text);
			Assert.IsNotNull (provider, "provider == null");
			act (provider);
			
			provider = CreateCtrlSpaceProvider (text);
			Assert.IsNotNull (provider, "provider == null");
			act (provider);
		}
		
		class TestFactory
		: ICompletionDataFactory
		{
			class CompletionData
			: ICompletionData
			{
				#region ICompletionData implementation
				public void AddOverload (ICompletionData data)
				{
					throw new NotImplementedException ();
				}

				public CompletionCategory CompletionCategory {
					get;
					set;
				}

				public string DisplayText {
					get;
					set;
				}

				public string Description {
					get;
					set;
				}

				public string CompletionText {
					get;
					set;
				}

				public DisplayFlags DisplayFlags {
					get;
					set;
				}

				public bool HasOverloads {
					get {
						throw new NotImplementedException ();
					}
				}

				public System.Collections.Generic.IEnumerable<ICompletionData> OverloadedData {
					get;
					set;
				}
				#endregion
				
				public CompletionData (string text)
				{
					DisplayText = CompletionText = Description = text;
				}
			}
			
			#region ICompletionDataFactory implementation
			public ICompletionData CreateEntityCompletionData (ICSharpCode.NRefactory.TypeSystem.IEntity entity)
			{
				return new CompletionData (entity.Name);
			}

			public ICompletionData CreateEntityCompletionData (ICSharpCode.NRefactory.TypeSystem.IEntity entity, string text)
			{
				return new CompletionData (text);
			}

			public ICompletionData CreateEntityCompletionData (ICSharpCode.NRefactory.TypeSystem.IUnresolvedEntity entity)
			{
				return new CompletionData (entity.Name);
			}
			

			public ICompletionData CreateTypeCompletionData (ICSharpCode.NRefactory.TypeSystem.IType type, string shortType)
			{
				return new CompletionData (shortType);
			}

			public ICompletionData CreateLiteralCompletionData (string title, string description, string insertText)
			{
				return new CompletionData (title);
			}

			public ICompletionData CreateNamespaceCompletionData (INamespace ns)
			{
				return new CompletionData (ns.Name);
			}

			public ICompletionData CreateVariableCompletionData (ICSharpCode.NRefactory.TypeSystem.IVariable variable)
			{
				return new CompletionData (variable.Name);
			}

			public ICompletionData CreateVariableCompletionData (ICSharpCode.NRefactory.TypeSystem.ITypeParameter parameter)
			{
				return new CompletionData (parameter.Name);
			}

			public ICompletionData CreateEventCreationCompletionData (string varName, ICSharpCode.NRefactory.TypeSystem.IType delegateType, ICSharpCode.NRefactory.TypeSystem.IEvent evt, string parameterDefinition, ICSharpCode.NRefactory.TypeSystem.IUnresolvedMember currentMember, ICSharpCode.NRefactory.TypeSystem.IUnresolvedTypeDefinition currentType)
			{
				return new CompletionData (varName);
			}

			public ICompletionData CreateNewOverrideCompletionData (int declarationBegin, ICSharpCode.NRefactory.TypeSystem.IUnresolvedTypeDefinition type, ICSharpCode.NRefactory.TypeSystem.IMember m)
			{
				return new CompletionData (m.Name);
			}
			
			public ICompletionData CreateNewPartialCompletionData (int declarationBegin, IUnresolvedTypeDefinition type, IUnresolvedMember m)
			{
				return new CompletionData (m.Name);
			}
			
			public System.Collections.Generic.IEnumerable<ICompletionData> CreateCodeTemplateCompletionData ()
			{
				return Enumerable.Empty<ICompletionData> ();
			}
			
			public IEnumerable<ICompletionData> CreatePreProcessorDefinesCompletionData ()
			{
				yield return new CompletionData ("DEBUG");
				yield return new CompletionData ("TEST");
			}
			#endregion
		}
		
		public static IUnresolvedAssembly SystemAssembly { get { return systemAssembly.Value; } }
		static readonly Lazy<IUnresolvedAssembly> systemAssembly = new Lazy<IUnresolvedAssembly>(
			delegate {
			return new CecilLoader().LoadAssemblyFile(typeof(System.ComponentModel.BrowsableAttribute).Assembly.Location);
		});
		
		public static CSharpCompletionEngine CreateEngine(string text, out int cursorPosition, params IUnresolvedAssembly[] references)
		{
			string parsedText;
			string editorText;
			cursorPosition = text.IndexOf('$');
			int endPos = text.IndexOf('$', cursorPosition + 1);
			if (endPos == -1) {
				if (cursorPosition < 0) {
					parsedText = editorText = text;
				} else {
					parsedText = editorText = text.Substring(0, cursorPosition) + text.Substring(cursorPosition + 1);
				}
			} else {
					parsedText = text.Substring(0, cursorPosition) + new string(' ', endPos - cursorPosition) + text.Substring(endPos + 1);
					editorText = text.Substring(0, cursorPosition) + text.Substring(cursorPosition + 1, endPos - cursorPosition - 1) + text.Substring(endPos + 1);
					cursorPosition = endPos - 1; 
			}
			var doc = new ReadOnlyDocument(editorText);

			IProjectContent pctx = new CSharpProjectContent();
			var refs = new List<IUnresolvedAssembly> { CecilLoaderTests.Mscorlib, CecilLoaderTests.SystemCore, SystemAssembly };
			if (references != null)
				refs.AddRange (references);

			pctx = pctx.AddAssemblyReferences(refs);

			var syntaxTree = new CSharpParser().Parse(parsedText, "program.cs");
			syntaxTree.Freeze();

			var unresolvedFile = syntaxTree.ToTypeSystem();
			pctx = pctx.AddOrUpdateFiles(unresolvedFile);

			var cmp = pctx.CreateCompilation();
			var loc = cursorPosition > 0 ? doc.GetLocation(cursorPosition) : new TextLocation (1, 1);

			var rctx = new CSharpTypeResolveContext(cmp.MainAssembly);
			rctx = rctx.WithUsingScope(unresolvedFile.GetUsingScope(loc).Resolve(cmp));

			var curDef = unresolvedFile.GetInnermostTypeDefinition(loc);
			if (curDef != null) {
					var resolvedDef = curDef.Resolve(rctx).GetDefinition();
					rctx = rctx.WithCurrentTypeDefinition(resolvedDef);
					var curMember = resolvedDef.Members.FirstOrDefault(m => m.Region.Begin <= loc && loc < m.BodyRegion.End);
					if (curMember != null) {
							rctx = rctx.WithCurrentMember(curMember);
					}
			}
			var mb = new DefaultCompletionContextProvider(doc, unresolvedFile);
			mb.AddSymbol ("TEST");
			var engine = new CSharpCompletionEngine(doc, mb, new TestFactory(), pctx, rctx);

			engine.EolMarker = Environment.NewLine;
			engine.FormattingPolicy = FormattingOptionsFactory.CreateMono();
			return engine;
		}

		public static CompletionDataList CreateProvider(string text, bool isCtrlSpace, params IUnresolvedAssembly[] references)
		{
			int cursorPosition;
			var engine = CreateEngine(text, out cursorPosition, references);
			var data = engine.GetCompletionData (cursorPosition, isCtrlSpace);
			
			return new CompletionDataList () {
				Data = data,
				AutoCompleteEmptyMatch = engine.AutoCompleteEmptyMatch,
				AutoSelect = engine.AutoSelect,
				DefaultCompletionString = engine.DefaultCompletionString
			};
		}
		
		Tuple<ReadOnlyDocument, CSharpCompletionEngine> GetContent(string text, SyntaxTree syntaxTree)
		{
			var doc = new ReadOnlyDocument(text);
			IProjectContent pctx = new CSharpProjectContent();
			pctx = pctx.AddAssemblyReferences(new [] { CecilLoaderTests.Mscorlib, CecilLoaderTests.SystemCore });
			var unresolvedFile = syntaxTree.ToTypeSystem();
			
			pctx = pctx.AddOrUpdateFiles(unresolvedFile);
			var cmp = pctx.CreateCompilation();
			
			var mb = new DefaultCompletionContextProvider(doc, unresolvedFile);
			var engine = new CSharpCompletionEngine (doc, mb, new TestFactory (), pctx, new CSharpTypeResolveContext (cmp.MainAssembly));
			engine.EolMarker = Environment.NewLine;
			engine.FormattingPolicy = FormattingOptionsFactory.CreateMono ();
			return Tuple.Create (doc, engine);
		}
		
		static CompletionDataList CreateProvider (string text, SyntaxTree syntaxTree, CSharpCompletionEngine engine, ReadOnlyDocument doc, TextLocation loc)
		{
			var cursorPosition = doc.GetOffset (loc);
			
			var data = engine.GetCompletionData (cursorPosition, true);
			
			return new CompletionDataList () {
				Data = data,
				AutoCompleteEmptyMatch = engine.AutoCompleteEmptyMatch,
				AutoSelect = engine.AutoSelect,
				DefaultCompletionString = engine.DefaultCompletionString
			};
		}
		
		public static void CheckObjectMembers (CompletionDataList provider)
		{
			Assert.IsNotNull (provider.Find ("Equals"), "Method 'System.Object.Equals' not found.");
			Assert.IsNotNull (provider.Find ("GetHashCode"), "Method 'System.Object.GetHashCode' not found.");
			Assert.IsNotNull (provider.Find ("GetType"), "Method 'System.Object.GetType' not found.");
			Assert.IsNotNull (provider.Find ("ToString"), "Method 'System.Object.ToString' not found.");
		}
		
		public static void CheckProtectedObjectMembers (CompletionDataList provider)
		{
			CheckObjectMembers (provider);
			Assert.IsNotNull (provider.Find ("MemberwiseClone"), "Method 'System.Object.MemberwiseClone' not found.");
		}
		
		public static void CheckStaticObjectMembers (CompletionDataList provider)
		{
			Assert.IsNotNull (provider.Find ("Equals"), "Method 'System.Object.Equals' not found.");
			Assert.IsNotNull (provider.Find ("ReferenceEquals"), "Method 'System.Object.ReferenceEquals' not found.");
		}
		
		class TestLocVisitor
		: ICSharpCode.NRefactory.CSharp.DepthFirstAstVisitor
		{
			public List<Tuple<TextLocation, string>> output = new List<Tuple<TextLocation, string>> ();
			
			public override void VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression)
			{
				output.Add (Tuple.Create (memberReferenceExpression.MemberNameToken.StartLocation, memberReferenceExpression.MemberName));
			}
			
			public override void VisitIdentifierExpression (IdentifierExpression identifierExpression)
			{
				output.Add (Tuple.Create (identifierExpression.StartLocation, identifierExpression.Identifier));
			}
		}
		
		[Ignore("TODO")]
		[Test()]
		public void TestLoadAllTests ()
		{
			int found = 0;
			int missing = 0;
			int exceptions = 0;
			int i = 0;
			foreach (var file in Directory.EnumerateFiles ("/Users/mike/work/mono/mcs/tests", "*.cs")) {
				if (i++ > 2)
					break;
				if (i <= 2)
					continue;
				var text = File.ReadAllText (file, Encoding.Default);
				try {
					var unit = new CSharpParser ().Parse (text, file);
					
					var cnt = GetContent (text, unit);
					
					var visitor = new TestLocVisitor ();
					unit.AcceptVisitor (visitor);
					foreach (var loc in visitor.output) {
						var provider = CreateProvider (text, unit, cnt.Item2, cnt.Item1, loc.Item1);
						if (provider.Find (loc.Item2) != null) {
							found++;
						} else {
							missing++;
						}
					}
				} catch (Exception e) {
					Console.WriteLine ("Exception in:" + file  + "/" + e);
					exceptions++;
				}
			}
			Console.WriteLine ("Found:" + found);
			Console.WriteLine ("Missing:" + missing);
			Console.WriteLine ("Exceptions:" + exceptions);
			if (missing > 0)
				Assert.Fail ();
		}
			
		[Test()]
		public void TestSimpleCodeCompletion ()
		{
			CompletionDataList provider = CreateProvider (
@"class Test { public void TM1 () {} public void TM2 () {} public int TF1; }
class CCTest {
void TestMethod ()
{
	Test t;
	$t.$
}
}
");
			Assert.IsNotNull (provider);
			Assert.AreEqual (7, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("TM1"));
			Assert.IsNotNull (provider.Find ("TM2"));
			Assert.IsNotNull (provider.Find ("TF1"));
		}

		[Test()]
		public void TestSimpleInterfaceCodeCompletion ()
		{
			CompletionDataList provider = CreateProvider (
@"interface ITest { void TM1 (); void TM2 (); int TF1 { get; } }
class CCTest {
void TestMethod ()
{
	ITest t;
	$t.$
}
}
");
			Assert.IsNotNull (provider);
			Assert.AreEqual (7, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("TM1"));
			Assert.IsNotNull (provider.Find ("TM2"));
			Assert.IsNotNull (provider.Find ("TF1"));
		}

		/// <summary>
		/// Bug 399695 - Code completion not working with an enum in a different namespace
		/// </summary>
		[Test()]
		public void TestBug399695 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace Other { enum TheEnum { One, Two } }
namespace ThisOne { 
        public class Test {
                public Other.TheEnum TheEnum {
                        set { }
                }

                public void TestMethod () {
                        $TheEnum = $
                }
        }
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("Other.TheEnum"), "Other.TheEnum not found.");
		}
		
		[Test()]
		public void TestInnerEnum ()
		{
			var provider = CreateProvider (
@"class Other { 
	public enum TheEnum { One, Two }
	public Other (TheEnum e) { }
}

public class Test {
	public void TestMethod () {
		$new Other (O$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("Other.TheEnum"), "'Other.TheEnum' not found.");
		}

		
		
		/// <summary>
		/// Bug 318834 - autocompletion kicks in when inputting decimals
		/// </summary>
		[Test()]
		public void TestBug318834 ()
		{
			CompletionDataList provider = CreateProvider (
@"class T
{
        static void Main ()
        {
                $decimal foo = 0.$
        }
}

");
			Assert.IsTrue (provider == null || provider.Count == 0);
		}
		
		[Test()]
		public void TestBug318834CaseB ()
		{
			CompletionDataList provider = CreateProvider (
@"class T
{
        static void Main ()
        {
                $decimal foo = 0.0.$
        }
}

");
			Assert.IsNotNull (provider);
		}

		/// <summary>
		/// Bug 321306 - Code completion doesn't recognize child namespaces
		/// </summary>
		[Test()]
		public void TestBug321306 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace a
{
	namespace b
	{
		public class c
		{
			public static int hi;
		}
	}
	
	public class d
	{
		public d ()
		{
			$b.$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("c"), "class 'c' not found.");
		}

		/// <summary>
		/// Bug 322089 - Code completion for indexer
		/// </summary>
		[Test()]
		public void TestBug322089 ()
		{
			CompletionDataList provider = CreateProvider (
@"class AClass
{
	public int AField;
	public int BField;
}

class Test
{
	public void TestMethod ()
	{
		AClass[] list = new AClass[0];
		$list[0].$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			for (int i = 0; i < provider.Count; i++) {
				var varname = provider [i];
				Console.WriteLine (varname.CompletionText);
			}
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("AField"), "field 'AField' not found.");
			Assert.IsNotNull (provider.Find ("BField"), "field 'BField' not found.");
		}
		
		/// <summary>
		/// Bug 323283 - Code completion for indexers offered by generic types (generics)
		/// </summary>
		[Test()]
		public void TestBug323283 ()
		{
			CompletionDataList provider = CreateProvider (
@"class AClass
{
	public int AField;
	public int BField;
}

class MyClass<T>
{
	public T this[int i] {
		get {
			return default (T);
		}
	}
}

class Test
{
	public void TestMethod ()
	{
		MyClass<AClass> list = new MyClass<AClass> ();
		$list[0].$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("AField"), "field 'AField' not found.");
			Assert.IsNotNull (provider.Find ("BField"), "field 'BField' not found.");
		}

		/// <summary>
		/// Bug 323317 - Code completion not working just after a constructor
		/// </summary>
		[Test()]
		public void TestBug323317 ()
		{
			CompletionDataList provider = CreateProvider (
@"class AClass
{
	public int AField;
	public int BField;
}

class Test
{
	public void TestMethod ()
	{
		$new AClass().$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("AField"), "field 'AField' not found.");
			Assert.IsNotNull (provider.Find ("BField"), "field 'BField' not found.");
		}
		
		/// <summary>
		/// Bug 325509 - Inaccessible methods displayed in autocomplete
		/// </summary>
		[Test()]
		public void TestBug325509 ()
		{
			CompletionDataList provider = CreateProvider (
@"class AClass
{
	public int A;
	public int B;
	
	protected int C;
	int D;
}

class Test
{
	public void TestMethod ()
	{
		AClass a;
		$a.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("A"), "field 'A' not found.");
			Assert.IsNotNull (provider.Find ("B"), "field 'B' not found.");
			Assert.IsNull (provider.Find ("C"), "field 'C' found, but shouldn't.");
			Assert.IsNull (provider.Find ("D"), "field 'D' found, but shouldn't.");
		}

		/// <summary>
		/// Bug 338392 - MD tries to use types when declaring namespace
		/// </summary>
		[Test()]
		public void TestBug338392 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace A
{
        class C
        {
        }
}

$namespace A.$
");
			if (provider != null)
				Assert.AreEqual (0, provider.Count);
		}

		/// <summary>
		/// Bug 427284 - Code Completion: class list shows the full name of classes
		/// </summary>
		[Test()]
		public void TestBug427284 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace TestNamespace
{
        class Test
        {
        }
}
class TestClass
{
	void Method ()
	{
		$TestNamespace.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found.");
		}

		/// <summary>
		/// Bug 427294 - Code Completion: completion on values returned by methods doesn't work
		/// </summary>
		[Test()]
		public void TestBug427294 ()
		{
			CompletionDataList provider = CreateProvider (
@"class TestClass
{
	public TestClass GetTestClass ()
	{
	}
}

class Test
{
	public void TestMethod ()
	{
		TestClass a;
		$a.GetTestClass ().$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (5, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("GetTestClass"), "method 'GetTestClass' not found.");
		}
		
		/// <summary>
		/// Bug 405000 - Namespace alias qualifier operator (::) does not trigger code completion
		/// </summary>
		[Test()]
		public void TestBug405000 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace A {
	class Test
	{
	}
}

namespace B {
	using foo = A;
	class C
	{
		public static void Main ()
		{
			$foo::$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found.");
		}
		
		/// <summary>
		/// Bug 427649 - Code Completion: protected methods shown in code completion
		/// </summary>
		[Test()]
		public void TestBug427649 ()
		{
			CompletionDataList provider = CreateProvider (
@"class BaseClass
{
	protected void ProtecedMember ()
	{
	}
}

class C : BaseClass
{
	public static void Main ()
	{
		BaseClass bc;
		$bc.$
	}
}
");
			// protected members should not be displayed in this case.
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (4, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
		}
		
		/// <summary>
		/// Bug 427734 - Code Completion issues with enums
		/// </summary>
		[Test()]
		public void TestBug427734A ()
		{
			CompletionDataList provider = CreateProvider (
@"public class Test
{
	public enum SomeEnum { a,b }
	
	public void Run ()
	{
		$Test.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (3, provider.Count);
			CodeCompletionBugTests.CheckStaticObjectMembers (provider); // 2 from System.Object
			Assert.IsNotNull (provider.Find ("SomeEnum"), "enum 'SomeEnum' not found.");
		}
		
		/// <summary>
		/// Bug 427734 - Code Completion issues with enums
		/// </summary>
		[Test()]
		public void TestBug427734B ()
		{
			CompletionDataList provider = CreateProvider (
@"public class Test
{
	public enum SomeEnum { a,b }
	
	public void Run ()
	{
		$SomeEnum.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("a"), "enum member 'a' not found.");
			Assert.IsNotNull (provider.Find ("b"), "enum member 'b' not found.");
		}
		
		/// <summary>
		/// Bug 431764 - Completion doesn't work in properties
		/// </summary>
		[Test()]
		public void TestBug431764 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"public class Test
{
	int number;
	public int Number {
		set { $this.number = $ }
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsTrue (provider.Count > 0, "provider should not be empty.");
			Assert.IsNotNull (provider.Find ("value"), "Should contain 'value'");
		}
		
		/// <summary>
		/// Bug 431797 - Code completion showing invalid options
		/// </summary>
		[Test()]
		public void TestBug431797A ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"public class Test
{
	private List<string> strings;
	$public $
}");
		
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("strings"), "should not contain 'strings'");
		}
		
		/// <summary>
		/// Bug 431797 - Code completion showing invalid options
		/// </summary>
		[Test()]
		public void TestBug431797B ()
		{
			CompletionDataList provider = CreateProvider (
@"public class Test
{
	public delegate string [] AutoCompleteHandler (string text, int pos);
	public void Method ()
	{
		Test t = new Test ();
		$t.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("AutoCompleteHandler"), "should not contain 'AutoCompleteHandler' delegate");
		}
		
		/// <summary>
		/// Bug 432681 - Incorrect completion in nested classes
		/// </summary>
		[Test()]
		public void TestBug432681 ()
		{
			CompletionDataList provider = CreateProvider (
@"

class C {
        public class D
        {
        }

        public void Method ()
        {
                $C.D c = new $
        }
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual ("D", provider.DefaultCompletionString, "Completion string is incorrect");
		}
		
		[Test()]
		public void TestGenericObjectCreation ()
		{
			CompletionDataList provider = CreateProvider (
@"
class List<T>
{
}
class Test{
	public void Method ()
	{
		$List<int> i = new $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsTrue (provider.Find ("List<int>") != null, "List<int> not found");
		}
		
		/// <summary>
		/// Bug 431803 - Autocomplete not giving any options
		/// </summary>
		[Test()]
		public void TestBug431803 ()
		{
			CompletionDataList provider = CreateProvider (
@"public class Test
{
	public string[] GetStrings ()
	{
		$return new $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("string[]"), "type string not found.");
		}

		/// <summary>
		/// Bug 434770 - No autocomplete on array types
		/// </summary>
		[Test()]
		public void TestBug434770 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Test
{
	public void AMethod ()
	{
		byte[] buffer = new byte[1024];
		$buffer.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Length"), "property 'Length' not found.");
		}
		
		/// <summary>
		/// Bug 439601 - Intellisense Broken For Partial Classes
		/// </summary>
		[Test()]
		public void TestBug439601 ()
		{
			CompletionDataList provider = CreateProvider (
@"
namespace MyNamespace
{
	partial class FormMain
	{
		private void Foo()
		{
			Bar();
		}
		
		private void Blah()
		{
			Foo();
		}
	}
}

namespace MyNamespace
{
	public partial class FormMain
	{
		public FormMain()
		{
		}
		
		private void Bar()
		{
			$this.$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
			Assert.IsNotNull (provider.Find ("Blah"), "method 'Blah' not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 1932 - [new resolver] fields don't show up unless prefixed with 'this.'
		/// </summary>
		[Test()]
		public void TestBug1932 ()
		{
			CombinedProviderTest (
@"
namespace MyNamespace
{
	partial class FormMain
	{
		int field1;
		string field2;
	}
}

namespace MyNamespace
{
	public partial class FormMain
	{
		private void Bar()
		{
			$f$
		}
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("field1"), "field 'field1' not found.");
				Assert.IsNotNull (provider.Find ("field2"), "field 'field2' not found.");
			});
		}
		
		/// <summary>
		/// Bug 1967 - [new resolver] Intellisense doesn't work
		/// </summary>
		[Test()]
		public void TestBug1967 ()
		{
			CombinedProviderTest (
@"
namespace MyNamespace
{
	partial class FormMain
	{
		FormMain field1;
		string field2;
	}
}

namespace MyNamespace
{
	public partial class FormMain
	{
		private void Bar()
		{
			$field1.$
		}
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("field1"), "field 'field1' not found.");
				Assert.IsNotNull (provider.Find ("field2"), "field 'field2' not found.");
			});
		}
		
		
		/// <summary>
		/// Bug 432434 - Code completion doesn't work with subclasses
		/// </summary>
		[Test()]
		public void TestBug432434 ()
		{
			CompletionDataList provider = CreateProvider (

@"public class Test
{
	public class Inner
	{
		public void Inner1 ()
		{
		}
		
		public void Inner2 ()
		{
		}
	}
	
	public void Run ()
	{
		Inner inner = new Inner ();
		$inner.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Inner1"), "Method inner1 not found.");
			Assert.IsNotNull (provider.Find ("Inner2"), "Method inner2 not found.");
		}

		/// <summary>
		/// Bug 432434A - Code completion doesn't work with subclasses
		/// </summary>
		[Test()]
		public void TestBug432434A ()
		{
			CompletionDataList provider = CreateProvider (

@"    public class E
        {
                public class Inner
                {
                        public void Method ()
                        {
                                Inner inner = new Inner();
                                $inner.$
                        }
                }
        }
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Method"), "Method 'Method' not found.");
		}
		
		/// <summary>
		/// Bug 432434B - Code completion doesn't work with subclasses
		/// </summary>
		[Test()]
		public void TestBug432434B ()
		{
			CompletionDataList provider = CreateProvider (

@"  public class E
        {
                public class Inner
                {
                        public class ReallyInner $: $
                        {

                        }
                }
        }
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("E"), "Class 'E' not found.");
			Assert.IsNotNull (provider.Find ("Inner"), "Class 'Inner' not found.");
			Assert.IsNull (provider.Find ("ReallyInner"), "Class 'ReallyInner' found, but shouldn't.");
		}
		

		/// <summary>
		/// Bug 436705 - code completion for constructors does not handle class name collisions properly
		/// </summary>
		[Test()]
		public void TestBug436705 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace System.Drawing {
	public class Point
	{
	}
}

public class Point
{
}

class C {

        public void Method ()
        {
                $System.Drawing.Point p = new $
        }
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual ("System.Drawing.Point", provider.DefaultCompletionString, "Completion string is incorrect");
		}
		
		/// <summary>
		/// Bug 439963 - Lacking members in code completion
		/// </summary>
		[Test()]
		public void TestBug439963 ()
		{
			CompletionDataList provider = CreateProvider (
@"public class StaticTest
{
	public void Test1()
	{}
	public void Test2()
	{}
	
	public static StaticTest GetObject ()
	{
	}
}

public class Test
{
	public void TestMethod ()
	{
		$StaticTest.GetObject ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test1"), "Method 'Test1' not found.");
			Assert.IsNotNull (provider.Find ("Test2"), "Method 'Test2' not found.");
			Assert.IsNull (provider.Find ("GetObject"), "Method 'GetObject' found, but shouldn't.");
		}

		/// <summary>
		/// Bug 441671 - Finalisers show up in code completion
		/// </summary>
		[Test()]
		public void TestBug441671 ()
		{
			CompletionDataList provider = CreateProvider (
@"class TestClass
{
	public TestClass (int i)
	{
	}
	public void TestMethod ()
	{
	}
	public ~TestClass ()
	{
	}
}

class AClass
{
	void AMethod ()
	{
		TestClass c;
		$c.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (5, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNull (provider.Find (".dtor"), "destructor found - but shouldn't.");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found.");
		}
		
		/// <summary>
		/// Bug 444110 - Code completion doesn't activate
		/// </summary>
		[Test()]
		public void TestBug444110 ()
		{
			CompletionDataList provider = CreateProvider (
@"using System;
using System.Collections.Generic;

namespace System.Collections.Generic {
	
	public class TemplateClass<T>
	{
		public T TestField;
	}
}

namespace CCTests
{
	
	public class Test
	{
		public TemplateClass<int> TemplateClass { get; set; }
	}
	
	class MainClass
	{
		public static void Main(string[] args)
		{
			Test t = new Test();
			$t.TemplateClass.$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (5, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("TestField"), "field 'TestField' not found.");
		}
		
		/// <summary>
		/// Bug 460234 - Invalid options shown when typing 'override'
		/// </summary>
		[Test()]
		public void TestBug460234 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class TestMe : System.Object
{
	$override $

	public override string ToString ()
	{
		return null; 
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (2, provider.Count);
			Assert.IsNull (provider.Find ("Finalize"), "method 'Finalize' found, but shouldn't.");
			Assert.IsNotNull (provider.Find ("GetHashCode"), "method 'GetHashCode' not found.");
			Assert.IsNotNull (provider.Find ("Equals"), "method 'Equals' not found.");
		}
		
		/// <summary>
		/// Bug 457003 - code completion shows variables out of scope
		/// </summary>
		[Test()]
		public void TestBug457003 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class A
{
	public void Test ()
	{
		if (true) {
			A st = null;
		}
		
		if (true) {
			int i = 0;
			$st.$
		}
	}
}
");
			if (provider != null)
				Assert.IsTrue (provider.Count == 0, "variable 'st' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 457237 - code completion doesn't show static methods when setting global variable
		/// </summary>
		[Test()]
		public void TestBug457237 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class Test
{
	public static double Val = 0.5;
}

class Test2
{
	$double dd = Test.$
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Val"), "field 'Val' not found.");
		}

		/// <summary>
		/// Bug 459682 - Static methods/properties don't show up in subclasses
		/// </summary>
		[Test()]
		public void TestBug459682 ()
		{
			CompletionDataList provider = CreateProvider (
@"public class BaseC
{
	public static int TESTER;
}
public class Child : BaseC
{
	public Child()
	{
		$Child.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TESTER"), "field 'TESTER' not found.");
		}
		
		/// <summary>
		/// Bug 466692 - Missing completion for return/break keywords after yield
		/// </summary>
		[Test()]
		public void TestBug466692 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class TestMe 
{
	public int Test ()
	{
		$yield $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (2, provider.Count);
			Assert.IsNotNull (provider.Find ("break"), "keyword 'break' not found");
			Assert.IsNotNull (provider.Find ("return"), "keyword 'return' not found");
		}
		
		/// <summary>
		/// Bug 467507 - No completion of base members inside explicit events
		/// </summary>
		[Test()]
		public void TestBug467507 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
using System;

class Test
{
	public void TestMe ()
	{
	}
	
	public event EventHandler TestEvent {
		add {
			$
		}
		remove {
			
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("TestMe"), "method 'TestMe' not found");
			Assert.IsNotNull (provider.Find ("value"), "keyword 'value' not found");
		}
		
		/// <summary>
		/// Bug 444643 - Extension methods don't show up on array types
		/// </summary>
		[Test()]
		public void TestBug444643 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
using System;
using System.Collections.Generic;

	static class ExtensionTest
	{
		public static bool TestExt<T> (this IList<T> list, T val)
		{
			return true;
		}
	}
	
	class MainClass
	{
		public static void Main(string[] args)
		{
			$args.$
		}
	}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TestExt"), "method 'TestExt' not found");
		}
		
		/// <summary>
		/// Bug 471935 - Code completion window not showing in MD1CustomDataItem.cs
		/// </summary>
		[Test()]
		public void TestBug471935 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
public class AClass
{
	public AClass Test ()
	{
		if (true) {
			AClass data;
			$data.$
			return data;
		} else if (false) {
			AClass data;
			return data;
		}
		return null;
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found");
		}
		
		/// <summary>
		/// Bug 471937 - Code completion of 'new' showing invorrect entries 
		/// </summary>
		[Test()]
		public void TestBug471937()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider(
@"
class B
{
}

class A
{
	public void Test()
	{
		int i = 5;
		i += 5;
		$A a = new $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
			Assert.AreEqual ("A", provider.DefaultCompletionString);
//			Assert.IsNull (provider.Find ("B"), "class 'B' found, but shouldn'tj.");
		}
		
		/// <summary>
		/// Bug 2268 - Potential omission in code completion
		/// </summary>
		[Test()]
		public void TestBug2268 ()
		{
			CombinedProviderTest (
@"
public class Outer
{
    static int value = 5;

    class Inner
    {
        void Method ()
        {
            $v$
        }
    }
}
", provider => {
				Assert.IsNotNull (provider.Find ("value"), "field 'value' not found.");
			});
		}
		
		
		/// <summary>
		/// Bug 2295 - [New Resolver] 'new' completion doesn't select the correct class name 
		/// </summary>
		[Test()]
		public void TestBug2295 ()
		{
			CombinedProviderTest (
@"
class A
{
	public void Test()
	{
		A a;
		$a = new $
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
				Assert.AreEqual ("A", provider.DefaultCompletionString);
			});
		}
		
		
	
		
		
		/// <summary>
		/// Bug 2061 - Typing 'new' in a method all does not offer valid completion
		/// </summary>
		[Test()]
		public void TestBug2061 ()
		{
			CombinedProviderTest (
@"
class A
{
	void CallTest(A a)
	{
	}
	public void Test()
	{
		$CallTest(new $
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
				Assert.AreEqual ("A", provider.DefaultCompletionString);
			});
		}
	
		[Test()]
		public void TestBug2061Case2 ()
		{
			CombinedProviderTest (
@"
class A
{
	void CallTest(int i, string s, A a)
	{
	}

	public void Test()
	{
		$CallTest(5, """", new $
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
				Assert.AreEqual ("A", provider.DefaultCompletionString);
			});
		}
		
		/// <summary>
		/// Bug 2788 - Locals do not show up inside the 'for' statement context
		/// </summary>
		[Test()]
		public void TestBug2788 ()
		{
			CombinedProviderTest (
@"
class A
{
	public void Test()
	{
		
		var foo = new byte[100];
		$for (int i = 0; i < f$
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("foo"), "'foo' not found.");
				Assert.IsNotNull (provider.Find ("i"), "'i' not found.");
			});
		}
		
		/// <summary>
		/// Bug 2800 - Finalize is offered as a valid completion target
		/// </summary>
		[Test()]
		public void TestBug2800 ()
		{
			CombinedProviderTest (
@"
class A
{
	public void Test()
	{
		$this.$
	}
}
", provider => {
				Assert.IsNull (provider.Find ("Finalize"), "'Finalize' found.");
			});
		}
		
		[Test()]
		public void TestBug2800B ()
		{
			CombinedProviderTest (
@"
class A
{
	$public override $
}
", provider => {
				Assert.IsNotNull (provider.Find ("ToString"), "'ToString' not found.");
				Assert.IsNull (provider.Find ("Finalize"), "'Finalize' found.");
			});
		}
		[Test()]
		public void TestOverrideCompletion ()
		{
			CombinedProviderTest (
@"using System;

class Base
{

	public virtual int Property { get;}
	public virtual int Method () { }
	public virtual event EventHandler Event;
	public virtual int this[int i] { get { } }
}


class A : Base
{
	$public override $
}
", provider => {
				Assert.IsNotNull (provider.Find ("Property"), "'Property' not found.");
				Assert.IsNotNull (provider.Find ("Method"), "'Method' not found.");
				Assert.IsNotNull (provider.Find ("Event"), "'Event' not found.");
				Assert.IsNotNull (provider.Find ("ToString"), "'Event' not found.");
				Assert.IsNotNull (provider.Find ("GetHashCode"), "'GetHashCode' not found.");
				Assert.IsNotNull (provider.Find ("Equals"), "'Equals' not found.");
				Assert.AreEqual (7, provider.Count);
			});
		}
		
		
		/// <summary>
		/// Bug 3370 -MD ignores member hiding
		/// </summary>
		[Test()]
		public void TestBug3370 ()
		{
			CombinedProviderTest (
@"
class A
{
	$public override $
}
", provider => {
				Assert.IsNotNull (provider.Find ("ToString"), "'ToString' not found.");
				Assert.IsNull (provider.Find ("Finalize"), "'Finalize' found.");
			});
		}
		
		/// <summary>
		/// Bug 2793 - op_Equality should not be offered in the completion list
		/// </summary>
		[Test()]
		public void Test2793 ()
		{
			CombinedProviderTest (
@"
using System;

public class MyClass
{
    public class A
    {
        public event EventHandler MouseClick;
    }

    public class B : A
    {
        public new event EventHandler MouseClick;
    }

    public class C : B
    {
        public new void MouseClick ()
        {
        }
    }

    static public void Main ()
    {
        C myclass = new C ();
        $myclass.$
    }
}", provider => {
				Assert.AreEqual (1, provider.Data.Where(c => c.DisplayText == "MouseClick").Count ());
			});
		}
		
		/// <summary>
		/// Bug 2798 - Unnecessary namespace qualification being prepended
		/// </summary>
		[Test()]
		public void Test2798 ()
		{
			CombinedProviderTest (
@"
using System;

namespace Foobar
{
    class MainClass
    {
        public enum Foo
        {
            Value1,
            Value2
        }

        public class Test
        {
            Foo Foo {
                get; set;
            }

            public static void Method (Foo foo)
            {
                $if (foo == F$
            }
        }
    }
}
", provider => {
				Assert.IsNull (provider.Find ("MainClass.Foo"), "'MainClass.Foo' found.");
				Assert.IsNotNull (provider.Find ("Foo"), "'Foo' not found.");
				Assert.IsNotNull (provider.Find ("Foo.Value1"), "'Foo.Value1' not found.");
				Assert.IsNotNull (provider.Find ("Foo.Value2"), "'Foo.Value2' not found.");
			});
		}
		
		
		/// <summary>
		/// Bug 2799 - No completion offered when declaring fields in a class
		/// </summary>
		[Test()]
		public void TestBug2799 ()
		{
			CombinedProviderTest (
@"namespace Foobar
{
    class MainClass
    {
        public enum Foo
        {
            Value1,
            Value2
        }
    }


    public class Second
    {
        $MainClass.$
    }
}

", provider => {
				Assert.IsNotNull (provider.Find ("Foo"), "'Foo' not found.");
			});
		}
		
		/// <summary>
		/// Bug 3371 - MD intellisense ignores namespace aliases
		/// </summary>
		[Test()]
		public void TestBug3371 ()
		{
			CombinedProviderTest (
@"namespace A
{
    using Base = B.Color;

    class Color
    {
        protected Base Base
        {
            get { return Base.Blue; }
        }

        protected Base NewBase {
            get {
                $return Base.$
            }
        }

        public static void Main ()
        {
        }
    }
}

namespace B
{
    public struct Color
    {
        public static Color Blue = new Color ();

        public static Color From (int i)
        {
            return new Color ();
        }
    }
}
", provider => {
				Assert.IsNotNull (provider.Find ("Blue"), "'Blue' not found.");
				Assert.IsNotNull (provider.Find ("From"), "'From' not found.");
			});
		}
		
		[Test()]
		public void TestNewInConstructor ()
		{
			CombinedProviderTest (
@"
class CallTest
{
	public CallTest(int i, string s, A a)
	{

	}
}

class A
{


	public void Test()
	{
		$new CallTest(5, """", new $
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
				Assert.AreEqual ("A", provider.DefaultCompletionString);
			});
		}		
		
		/// <summary>
		/// Bug 473686 - Constants are not included in code completion
		/// </summary>
		[Test()]
		public void TestBug473686 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
class ATest
{
	const int TESTCONST = 0;

	static void Test()
	{
		$$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("TESTCONST"), "constant 'TESTCONST' not found.");
		}
		
		/// <summary>
		/// Bug 473849 - Classes with no visible constructor shouldn't appear in "new" completion
		/// </summary>
		[Test()]
		public void TestBug473849 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class TestB
{
	protected TestB()
	{
	}
}

class TestC : TestB
{
	internal TestC ()
	{
	}
}

class TestD : TestB
{
	public TestD ()
	{
	}
}

class TestE : TestD
{
	protected TestE ()
	{
	}
}

class Test : TestB
{
	void TestMethod ()
	{
		$TestB test = new $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNull (provider.Find ("TestE"), "class 'TestE' found, but shouldn't.");
			Assert.IsNotNull (provider.Find ("TestD"), "class 'TestD' not found");
			Assert.IsNotNull (provider.Find ("TestC"), "class 'TestC' not found");
			Assert.IsNotNull (provider.Find ("TestB"), "class 'TestB' not found");
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found");
		}
		
		/// <summary>
		/// Bug 474199 - Code completion not working for a nested class
		/// </summary>
		[Test()]
		public void TestBug474199A ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class InnerTest
{
	public class Inner
	{
		public void Test()
		{
		}
	}
}

public class ExtInner : InnerTest
{
}

class Test
{
	public void TestMethod ()
	{
		var inner = new ExtInner.Inner ();
		$inner.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found");
		}
		
		/// <summary>
		/// Bug 3438 - [New Resolver] Local var missing in code completion
		/// </summary>
		[Test()]
		public void Test3438 ()
		{
			CombinedProviderTest (
@"
using System;
using System.Text;

class C
{
	void GetElementXml (int indent)
	{
		StringBuilder sb = new StringBuilder ();
		if (indent == 0)
			sb.Append ("" xmlns:android=\""http://schemas.android.com/apk/res/android\"""");
		
		if (indent != 0) {
			string data;
			$d$
		}
	}	
}", provider => {
				Assert.IsNotNull (provider.Find ("data"), "'data' not found.");
			});
		}
		
		/// <summary>
		/// Bug 3436 - [New Resolver] Type missing in return type completion
		/// </summary>
		[Test()]
		public void Test3436 ()
		{
			CombinedProviderTest (
@"
namespace A 
{
	public class SomeClass {}
}

namespace Foo 
{
	public partial class Bar {}
}

namespace Foo 
{
	using A;
	public partial class Bar {
		$S$
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("SomeClass"), "'SomeClass' not found.");
			});
		}
		
		
		/// <summary>
		/// Bug 474199 - Code completion not working for a nested class
		/// </summary>
		[Test()]
		public void TestBug474199B ()
		{
			var provider = ParameterCompletionTests.CreateProvider (
@"
public class InnerTest
{
	public class Inner
	{
		public Inner(string test)
		{
		}
	}
}

public class ExtInner : InnerTest
{
}

class Test
{
	public void TestMethod ()
	{
		$new ExtInner.Inner ($
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count, "There should be one overload");
			Assert.AreEqual (1, provider.GetParameterCount (0), "Parameter 'test' should exist");
		}
		
		/// <summary>
		/// Bug 350862 - Autocomplete bug with enums
		/// </summary>
		[Test()]
		public void TestBug350862 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public enum MyEnum {
	A,
	B,
	C
}

public class Test
{
	MyEnum item;
	public void Method (MyEnum val)
	{
		$item = $
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("val"), "parameter 'val' not found");
		}
		
		/// <summary>
		/// Bug 470954 - using System.Windows.Forms is not honored
		/// </summary>
		[Test()]
		public void TestBug470954 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Control
{
	public MouseButtons MouseButtons { get; set; }
}

public enum MouseButtons {
	Left, Right
}

public class SomeControl : Control
{
	public void Run ()
	{
		$MouseButtons m = MouseButtons.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Left"), "enum 'Left' not found");
			Assert.IsNotNull (provider.Find ("Right"), "enum 'Right' not found");
		}
		
		/// <summary>
		/// Bug 470954 - using System.Windows.Forms is not honored
		/// </summary>
		[Test()]
		public void TestBug470954_bis ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Control
{
	public string MouseButtons { get; set; }
}

public enum MouseButtons {
	Left, Right
}

public class SomeControl : Control
{
	public void Run ()
	{
		$int m = MouseButtons.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNull (provider.Find ("Left"), "enum 'Left' found");
			Assert.IsNull (provider.Find ("Right"), "enum 'Right' found");
		}
		
		
		
		/// <summary>
		/// Bug 487228 - No intellisense for implicit arrays
		/// </summary>
		[Test()]
		public void TestBug487228 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Test
{
	public void Method ()
	{
		var v = new [] { new Test () };
		$v[0].$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Method"), "method 'Method' not found");
		}
		
		/// <summary>
		/// Bug 487218 - var does not work with arrays
		/// </summary>
		[Test()]
		public void TestBug487218 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Test
{
	public void Method ()
	{
		var v = new Test[] { new Test () };
		$v[0].$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Method"), "method 'Method' not found");
		}
		
		/// <summary>
		/// Bug 487206 - Intellisense not working
		/// </summary>
		[Test()]
		public void TestBug487206 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class CastByExample
{
	static T Cast<T> (object obj, T type)
	{
		return (T) obj;
	}
	
	static void Main ()
	{
		var typed = Cast (o, new { Foo = 5 });
		$typed.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Foo"), "property 'Foo' not found");
		}

		/// <summary>
		/// Bug 487203 - Extension methods not working
		/// </summary>
		[Test()]
		public void TestBug487203 ()
		{
			CompletionDataList provider = CreateProvider (
@"
using System;
using System.Linq;
using System.Collections.Generic;


class Program 
{
	public void Foo ()
	{
		Program[] prgs;
		foreach (var prg in (from Program p in prgs select p)) {
			$prg.$
		}
	}
}		
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found");
		}
		
		/// <summary>
		/// Bug 491020 - Wrong typeof intellisense
		/// </summary>
		[Test()]
		public void TestBug491020 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class EventClass<T>
{
	public class Inner {}
	public delegate void HookDelegate (T del);
	public void Method ()
	{}
}

public class Test
{
	public static void Main ()
	{
		$EventClass<int>.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Inner"), "class 'Inner' not found.");
			Assert.IsNotNull (provider.Find ("HookDelegate"), "delegate 'HookDelegate' not found.");
			Assert.IsNull (provider.Find ("Method"), "method 'Method' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 491020 - Wrong typeof intellisense
		/// It's a different case when the class is inside a namespace.
		/// </summary>
		[Test()]
		public void TestBug491020B ()
		{
			CompletionDataList provider = CreateProvider (
@"

namespace A {
	public class EventClass<T>
	{
		public class Inner {}
		public delegate void HookDelegate (T del);
		public void Method ()
		{}
	}
}

public class Test
{
	public static void Main ()
	{
		$A.EventClass<int>.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Inner"), "class 'Inner' not found.");
			Assert.IsNotNull (provider.Find ("HookDelegate"), "delegate 'HookDelegate' not found.");
			Assert.IsNull (provider.Find ("Method"), "method 'Method' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 491019 - No intellisense for recursive generics
		/// </summary>
		[Test()]
		public void TestBug491019 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public abstract class NonGenericBase
{
	public abstract int this[int i] { get; }
}

public abstract class GenericBase<T> : NonGenericBase where T : GenericBase<T>
{
	T Instance { get { return default (T); } }

	public void Foo ()
	{
		$Instance.Instance.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Instance"), "property 'Instance' not found.");
			Assert.IsNull (provider.Find ("this"), "'this' found, but shouldn't.");
		}
		
		
				
		/// <summary>
		/// Bug 429034 - Class alias completion not working properly
		/// </summary>
		[Test()]
		public void TestBug429034 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
using Path = System.IO.Path;

class Test
{
	void Test ()
	{
		$$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Path"), "class 'Path' not found.");
		}	
		
		/// <summary>
		/// Bug 429034 - Class alias completion not working properly
		/// </summary>
		[Test()]
		public void TestBug429034B ()
		{
			CompletionDataList provider = CreateProvider (
@"
using Path = System.IO.Path;

class Test
{
	void Test ()
	{
		$Path.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("DirectorySeparatorChar"), "method 'PathTest' not found.");
		}
		
		[Test()]
		public void TestInvalidCompletion ()
		{
			CompletionDataList provider = CreateProvider (
@"
class TestClass
{
	public void TestMethod ()
	{
	}
}

class Test
{
	public void Foo ()
	{
		TestClass tc;
		$tc.garbage.$
	}
}
");
			if (provider != null)
				Assert.IsNull (provider.Find ("TestMethod"), "method 'TestMethod' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 510919 - Code completion does not show interface method when not using a local var 
		/// </summary>
		[Test()]
		public void TestBug510919 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Foo : IFoo 
{
	public void Bar () { }
}

public interface IFoo 
{
	void Bar ();
}

public class Program
{
	static IFoo GiveMeFoo () 
	{
		return new Foo ();
	}

	static void Main ()
	{
		$GiveMeFoo ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
			
		
			
		/// <summary>
		/// Bug 538208 - Go to declaration not working over a generic method...
		/// </summary>
		[Test()]
		public void TestBug538208 ()
		{
			// We've to test 2 expressions for this bug. Since there are 2 ways of accessing
			// members.
			// First: the identifier expression
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
class MyClass
{
	public string Test { get; set; }
	
	T foo<T>(T arg)
	{
		return arg;
	}

	public void Main(string[] args)
	{
		var myObject = foo<MyClass>(new MyClass());
		$myObject.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test"), "property 'Test' not found.");
			
			// now the member reference expression 
			provider = CreateCtrlSpaceProvider (
@"
class MyClass2
{
	public string Test { get; set; }
	
	T foo<T>(T arg)
	{
		return arg;
	}

	public void Main(string[] args)
	{
		var myObject = this.foo<MyClass2>(new MyClass2());
		$myObject.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test"), "property 'Test' not found.");
		}
		
		/// <summary>
		/// Bug 542976 resolution problem
		/// </summary>
		[Test()]
		public void TestBug542976 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class KeyValuePair<S, T>
{
	public S Key { get; set;}
	public T Value { get; set;}
}

class TestMe<T> : System.Collections.Generic.IEnumerable<T>
{
	public System.Collections.Generic.IEnumerator<T> GetEnumerator ()
	{
		throw new System.NotImplementedException();
	}

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
	{
		throw new System.NotImplementedException();
	}
}

namespace TestMe 
{
	class Bar
	{
		public int Field;
	}
	
	class Test
	{
		void Foo (TestMe<KeyValuePair<Bar, int>> things)
		{
			foreach (var thing in things) {
				$thing.Key.$
			}
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Field"), "field 'Field' not found.");
		}
		
		
		/// <summary>
		/// Bug 545189 - C# resolver bug
		/// </summary>
		[Test()]
		public void TestBug545189A ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class A<T>
{
	public class B
	{
		public T field;
	}
}

public class Foo
{
	public void Bar ()
	{
		A<Foo>.B baz = new A<Foo>.B ();
		$baz.field.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 549864 - Intellisense does not work properly with expressions
		/// </summary>
		[Test()]
		public void TestBug549864 ()
		{
			CompletionDataList provider = CreateProvider (
@"
delegate T MyFunc<S, T> (S t);

class TestClass
{
	public string Value {
		get;
		set;
	}
	
	public static object GetProperty<TType> (MyFunc<TType, object> expression)
	{
		return null;
	}
	private static object ValueProperty = TestClass.GetProperty<TestClass> ($x => x.$);
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Value"), "property 'Value' not found.");
		}
		
		
		/// <summary>
		/// Bug 550185 - Intellisence for extension methods
		/// </summary>
		[Test()]
		public void TestBug550185 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public interface IMyinterface<T> {
	T Foo ();
}

public static class ExtMethods {
	public static int MyCountMethod(this IMyinterface<string> i)
	{
		return 0;
	}
}

class TestClass
{
	void Test ()
	{
		IMyinterface<int> test;
		$test.$
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("MyCountMet2hod"), "method 'MyCountMethod' found, but shouldn't.");
		}
		
			
		/// <summary>
		/// Bug 553101 – Enum completion does not use type aliases
		/// </summary>
		[Test()]
		public void TestBug553101 ()
		{
			CompletionDataList provider = CreateProvider (
@"
namespace Some.Type 
{
	public enum Name { Foo, Bar }
}

namespace Test
{
	using STN = Some.Type.Name;
	
	public class Main
	{
		public void TestMe ()
		{
			$STN foo = $
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
		}
			
		/// <summary>
		/// Bug 555523 - C# code completion gets confused by extension methods with same names as properties
		/// </summary>
		[Test()]
		public void TestBug555523A ()
		{
			CompletionDataList provider = CreateProvider (
@"
class A
{
	public int AA { get; set; }
}

class B
{
	public int BB { get; set; }
}

static class ExtMethod
{
	public static A Extension (this MyClass myClass)
	{
		return null;
	}
}

class MyClass
{
	public B Extension {
		get;
		set;
	}
}

class MainClass
{
	public static void Main (string[] args)
	{
		MyClass myClass;
		$myClass.Extension ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("AA"), "property 'AA' not found.");
		}
		
		/// <summary>
		/// Bug 555523 - C# code completion gets confused by extension methods with same names as properties
		/// </summary>
		[Test()]
		public void TestBug555523B ()
		{
			CompletionDataList provider = CreateProvider (
@"
class A
{
	public int AA { get; set; }
}

class B
{
	public int BB { get; set; }
}

static class ExtMethod
{
	public static A Extension (this MyClass myClass)
	{
		return null;
	}
}

class MyClass
{
	public B Extension {
		get;
		set;
	}
}

class MainClass
{
	public static void Main (string[] args)
	{
		MyClass myClass;
		$myClass.Extension.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("BB"), "property 'BB' not found.");
		}
		
		
		/// <summary>
		/// Bug 561964 - Wrong type in tooltip when there are two properties with the same name
		/// </summary>
		[Test()]
		public void TestBug561964 ()
		{
			CompletionDataList provider = CreateProvider (
@"
interface A1 {
	int A { get; }
}
interface A2 {
	int B { get; }
}

interface IFoo {
	A1 Bar { get; }
}

class Foo : IFoo
{
	A1 IFoo.Bar { get { return null; } }
	public A2 Bar { get { return null; } }

	public static int Main (string[] args)
	{
		$new Foo().Bar.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("B"), "property 'B' not found.");
		}
		
		
		/// <summary>
		/// Bug 568204 - Inconsistency in resolution
		/// </summary>
		[Test()]
		public void TestBug568204 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Style 
{
	public static Style TestMe ()
	{
		return new Style ();
	}
	
	public void Print ()
	{
		System.Console.WriteLine (""Hello World!"");
	}
}

public class Foo
{
	public Style Style { get; set;} 
	
	public void Bar ()
	{
		$Style.TestMe ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Print"), "method 'Print' not found.");
		}
		
		/// <summary>
		/// Bug 577225 - Inconsistent autocomplete on returned value of generic method.
		/// </summary>
		[Test()]
		public void TestBug577225 ()
		{
			CompletionDataList provider = CreateProvider (
@"
using Foo;
	
namespace Foo 
{
	public class FooBar
	{
		public void Bar ()
		{
		}
	}
}

namespace Other 
{
	public class MainClass
	{
		public static T Test<T> ()
		{
			return default (T);
		}
			
		public static void Main (string[] args)
		{
			$Test<FooBar> ().$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		
		
		/// <summary>
		/// Bug 582017 - C# Generic Type Constraints
		/// </summary>
		[Test()]
		public void TestBug582017 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class Bar
{
	public void MyMethod ()
	{
	}
}

class Foo
{
	public static void Test<T> (T theObject) where T : Bar
	{
		$theObject.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("MyMethod"), "method 'MyMethod' not found.");
		}
		
		/// <summary>
		/// Bug 586304 - Intellisense does not show several linq extenion methods when using nested generic type
		/// </summary>
		[Test()]
		public void TestBug586304 ()
		{
			CompletionDataList provider = CreateProvider (
@"
using System;
using System.Collections.Generic;

public static class ExtMethods
{
	public static bool IsEmpty<T> (this IEnumerable<T> v)
	{
		return !v.Any ();
	}
}

public class Lazy<T> {}

public class IntelliSenseProblems
{
    public IEnumerable<Lazy<T>> GetLazies<T>()
    {
        return Enumerable.Empty<Lazy<T>>();
    }
}

public class Test
{ 
   void test ()
   {
		var values = new IntelliSenseProblems ();
		$var x = values.GetLazies<string> ().$
   }
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("IsEmpty"), "method 'IsEmpty' not found.");
		}
		
		/// <summary>
		/// Bug 586304 - Intellisense does not show several linq extenion methods when using nested generic type
		/// </summary>
		[Test()]
		public void TestBug586304B ()
		{
			CompletionDataList provider = CreateProvider (
@"
public delegate S Func<T, S> (T t);

public class Lazy<T> {
	public virtual bool IsLazy ()
	{
		return true;
	}
}

static class ExtMethods
{
	public static T Where<T>(this Lazy<T> t, Func<T, bool> pred)
	{
		return default (T);
	}
}

class MyClass
{
	public void Test()
	{
		Lazy<Lazy<MyClass>> c; 
		$c.Where (x => x.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("Test"), "method 'Test' found, but shouldn't.");
			Assert.IsNotNull (provider.Find ("IsLazy"), "method 'IsLazy' not found.");
		}
		
		
		/// <summary>
		/// Bug 587543 - Intellisense ignores interface constraints
		/// </summary>
		[Test()]
		public void TestBug587543 ()
		{
			CompletionDataList provider = CreateProvider (
@"
interface ITest
{
	void Foo ();
}

class C
{
	void Test<T> (T t) where T : ITest
	{
		$t.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
		}

		
		/// <summary>
		/// Bug 587549 - Intellisense does not work with override constraints
		/// </summary>
		[Test()]
		public void TestBug587549 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public interface ITest
{
	void Bar();
}

public class BaseClass
{
	public void Foo ()
	{}
}

public abstract class Printer
{
	public abstract void Print<T, U> (object x) where T : BaseClass, U where U : ITest;
}

public class PrinterImpl : Printer
{
	public override void Print<A, B> (object x)
	{
		A a;
		$a.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 588223 - Intellisense does not recognize nested generics correctly.
		/// </summary>
		[Test()]
		public void TestBug588223 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class Lazy<T> { public void Foo () {} }
class Lazy<T, S> { public void Bar () {} }

class Test
{
	public object Get ()
	{
		return null;
	}
	
	public Lazy<T> Get<T> ()
	{
		return null;
	}

	public Lazy<T, TMetaDataView> Get<T, TMetaDataView> ()
	{
		return null;
	}
	
	public Test ()
	{
		Test t = new Test ();
		var bug = t.Get<string, string> ();
		$bug.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 592120 - Type resolver bug with this.Property[]
		/// </summary>
		[Test()]
		public void TestBug592120 ()
		{
			CompletionDataList provider = CreateProvider (
@"

interface IBar
{
	void Test ();
}

class Foo
{
	public IBar[] X { get; set; }

	public void Bar ()
	{
		var y = this.X;
		$y.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("Test"), "method 'Test' found, but shouldn't.");
		}
		
		
		/// <summary>
		/// Bug 576354 - Type inference failure
		/// </summary>
		[Test()]
		public void TestBug576354 ()
		{
			CompletionDataList provider = CreateProvider (
@"
delegate T Func<S, T> (S s);

class Foo
{
	string str;
	
	public Foo (string str)
	{
		this.str = str;
	}
	
	public void Bar () 
	{
		System.Console.WriteLine (str);
	}
}

class MyTest
{
	static T Test<T> (Func<string, T> myFunc)
	{
		return myFunc (""Hello World"");
	}
	
	public static void Main (string[] args)
	{
		var result = Test (str => new Foo (str));
		$result.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 534680 - LINQ keywords missing from Intellisense
		/// </summary>
		[Test()]
		public void TestBug534680 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
class Foo
{
	public static void Main (string[] args)
	{
		$from str in args $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("select"), "keyword 'select' not found.");
		}
		
		/// <summary>
		/// Bug 610006 - Intellisense gives members of return type of functions even when that function isn't invoked
		/// </summary>
		[Test()]
		public void TestBug610006 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class MainClass
{
	public MainClass FooBar ()
	{
	}
	
	public void Test ()
	{
		$FooBar.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("FooBar"), "method 'FooBar' found, but shouldn't.");
		}
		
		
		/// <summary>
		/// Bug 614045 - Types hidden by members are not formatted properly by ambience
		/// </summary>
		[Test()]
		public void TestBug614045 ()
		{
			CompletionDataList provider = CreateProvider (
@"
namespace A
{
	enum Foo
	{
		One,
		Two,
		Three
	}
}

namespace B
{
	using A;
	
	public class Baz
	{
		public string Foo;
		
		void Test (Foo a)
		{
			$switch (a) {
			case $
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("Foo"), "enum 'Foo' found, but shouldn't.");
			Assert.IsNotNull (provider.Find ("A.Foo"), "enum 'A.Foo' not found.");
		}
		
		/// <summary>
		/// Bug 615992 - Intellisense broken when calling generic method.
		/// </summary>
		[Test()]
		public void TestBug615992 ()
		{
			CompletionDataList provider = CreateProvider (
@"public delegate void Act<T> (T t);

public class Foo
{
	public void Bar ()
	{
	}
}

class TestBase
{
	protected void Method<T> (Act<T> action)
	{
	}
}

class Test : TestBase
{
	public Test ()
	{
		$Method<Foo> (f => f.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 625064 - Internal classes aren't suggested for completion
		/// </summary>
		[Test()]
		public void TestBug625064 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"class Foo 
{
	class Bar { }
	$List<$
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "class 'Bar' not found.");
		}
		
		
		/// <summary>
		/// Bug 631875 - No Intellisense for arrays
		/// </summary>
		[Test()]
		public void TestBug631875 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"class C
{
	static void Main ()
	{
		var objects = new[] { new { X = (object)null }};
		$objects[0].$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("X"), "property 'X' not found.");
		}
		
		/// <summary>
		/// Bug 632228 - Wrong var inference
		/// </summary>
		[Test()]
		public void TestBug632228 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
class C {
	public void FooBar () {}
	public static void Main ()
	{
		var thingToTest = new[] { new C (), 22, new object(), string.Empty, null };
		$thingToTest[0].$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("FooBar"), "method 'FooBar' found, but shouldn't.");
		}

		/// <summary>
		/// Bug 632696 - No intellisense for constraints
		/// </summary>
		[Test()]
		public void TestBug632696 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
class Program
{
	void Foo ()
	{
	}

	static void Foo<T> () where T : Program
	{
		var s = new[] { default(T) };
		$s[0].$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
		}
		
		[Test()]
		public void TestCommentsWithWindowsEol ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider ("class TestClass\r\n{\r\npublic static void Main (string[] args) {\r\n// TestComment\r\n$args.$\r\n}\r\n}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("ToString"), "method 'ToString' not found.");
		}
		
		[Test()]
		public void TestGhostEntryBug ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
using System.IO;

class TestClass
{
	public Path Path {
		get;
		set;
	}
	
	void Test ()
	{
		$$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("System.IO.Path"), "'System.IO.Path' found but shouldn't.");
			Assert.IsNotNull (provider.Find ("Path"), "property 'Path' not found.");
		}
		
		
		/// <summary>
		/// Bug 648562 – Abstract members are allowed by base call
		/// </summary>
		[Test()]
		public void TestBug648562 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"using System;

abstract class A
{
    public abstract void Foo<T> (T type);
}

class B : A
{
    public override void Foo<U> (U type)
    {
        $base.$
    }
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("Foo"), "method 'Foo' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 633767 - Wrong intellisense for simple lambda
		/// </summary>
		[Test()]
		public void TestBug633767 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"using System;

public class E
{
	public int Foo { get; set; }
}

public class C
{
	delegate void D<T> (T t);
	
	static T M<T> (T t, D<T> a)
	{
		return t;
	}

	static void MethodArg (object o)
	{
	}

	public static int Main ()
	{
		D<object> action = l => Console.WriteLine (l);
		var b = M (new E (), action);
		$b.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("Foo"), "property 'Foo' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 616208 - Renaming a struct/class is renaming too much
		/// </summary>
		[Test()]
		public void TestBug616208 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"using System;

namespace System 
{
	public class Foo { public int Bar; };
}

namespace test.Util
{
	public class Foo { public string x; }
}

namespace Test
{
	public class A
	{
		public Foo X;
		
		public A ()
		{
			$X.$
		}
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "property 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 668135 - Problems with "new" completion
		/// </summary>
		[Test()]
		public void TestBug668135a ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"public class A
{
	public A ()
	{
		string test;
		$Console.WriteLine (test = new $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("string"), "class 'string' not found.");
		}
		
		/// <summary>
		/// Bug 668453 - var completion infers var type too eagerly
		/// </summary>
		[Test()]
		public void TestBug668453 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"public class Test
{
	private void FooBar ()
	{
		$var str = new $
		FooBar ();
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("FooBar"), "method 'FooBar' found.");
		}
		
		/// <summary>
		/// Bug 669285 - Extension method on T[] shows up on T
		/// </summary>
		[Test()]
		public void TestBug669285 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"static class Ext
{
	public static void Foo<T> (this T[] t)
	{
	}
}

public class Test<T>
{
	public void Foo ()
	{
		T t;
		$t.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("Foo"), "method 'Foo' found.");
		}
		
		public void TestBug669285B ()
		{
			var provider = CreateCtrlSpaceProvider (
@"static class Ext
{
	public static void Foo<T> (this T[] t)
	{
	}
}

public class Test<T>
{
	public void Foo ()
	{
		T[] t;
		$t.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
		}
		
		
		/// <summary>
		/// Bug 669818 - Autocomplete missing for new nested class
		/// </summary>
		[Test()]
		public void TestBug669818 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"using System;
public class Foo
{
    public class Bar
    {
    }
	public static void FooBar () {}
}
class TestNested
{
    public static void Main (string[] args)
    {
        $new Foo.$
    }
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "class 'Bar' not found.");
			Assert.IsNull (provider.Find ("FooBar"), "method 'FooBar' found.");
		}
		
		/// <summary>
		/// Bug 674514 - foreach value should not be in the completion list
		/// </summary>
		[Test()]
		public void TestBug674514 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"using System;
using System.Linq;
using System.Collections.Generic;

class Foo
{
	public static void Main (string[] args)
	{
		$foreach (var arg in $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("args"), "parameter 'args' not found.");
			Assert.IsNull (provider.Find ("arg"), "variable 'arg' found.");
		}
		
		[Test()]
		public void TestBug674514B ()
		{
			var provider = CreateCtrlSpaceProvider (
@"using System;
using System.Linq;
using System.Collections.Generic;

class Foo
{
	public static void Main (string[] args)
	{
		$foreach (var arg in args) 
			Console.WriteLine ($
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("args"), "parameter 'args' not found.");
			Assert.IsNotNull (provider.Find ("arg"), "variable 'arg' not found.");
		}
		
		/// <summary>
		/// Bug 675436 - Completion is trying to complete symbol names in declarations
		/// </summary>
		[Test()]
		public void TestBug675436_LocalVar ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"class Test
{
    public static void Main (string[] args)
    {
        $int test = $
    }
}
");
			Assert.IsNull (provider.Find ("test"), "name 'test' found.");
		}
		
		/// <summary>
		/// Bug 675956 - Completion in for loops is broken
		/// </summary>
		[Test()]
		public void TestBug675956 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"class Test
{
    public static void Main (string[] args)
    {
        $for (int i = 0; $
    }
}
");
			Assert.IsNotNull (provider.Find ("i"), "variable 'i' not found.");
		}
		
		/// <summary>
		/// Bug 675956 - Completion in for loops is broken
		/// </summary>
		[Test()]
		public void TestBug675956Case2 ()
		{
			CompletionDataList provider = CreateProvider (
@"class Test
{
    public static void Main (string[] args)
    {
        $for (int i = 0; i$
    }
}
");
			Assert.IsNotNull (provider.Find ("i"), "variable 'i' not found.");
		}
		
		/// <summary>
		/// Bug 676311 - auto completion too few proposals in fluent API (Moq)
		/// </summary>
		[Test()]
		public void TestBug676311 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"using System;
using System.Linq;
using System.Linq.Expressions;

namespace Test
{
	public interface IFoo<T>
	{
		void Foo1 ();
	}

	public interface IFoo<T, S>
	{
		void Foo2 ();
	}
	
	public class Test<T>
	{
		public IFoo<T> TestMe (Expression<Action<T>> act)
		{
			return null;
		}
		
		public IFoo<T, S> TestMe<S> (Expression<Func<S, T>> func)
		{
			return null;
		}
		
		public string TestMethod (string str)
		{
			return str;
		}
	}
	
	class MainClass
	{
		public static void Main (string[] args)
		{
			var t = new Test<string> ();
			var s = t.TestMe (x => t.TestMethod (x));
			$s.$
		}
	}
}");
			Assert.IsNotNull (provider.Find ("Foo1"), "method 'Foo1' not found.");
		}
		/// <summary>
		/// Bug 676311 - auto completion too few proposals in fluent API (Moq)
		/// </summary>
		[Test()]
		public void TestBug676311B ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"using System;
using System.Linq;
using System.Linq.Expressions;

namespace Test
{
	public interface IFoo<T>
	{
		void Foo1 ();
	}

	public interface IFoo<T, S>
	{
		void Foo2 ();
	}
	
	public class Test<T>
	{
		public IFoo<T> TestMe (Expression<Action<T>> act)
		{
			return null;
		}
		
		public IFoo<T, S> TestMe<S> (Expression<Func<S, T>> func)
		{
			return null;
		}
		
		public string TestMethod (string str)
		{
			return str;
		}
	}
	
	class MainClass
	{
		public static void Main (string[] args)
		{
			var t = new Test<string> ();
			var s = t.TestMe<string> (x => t.TestMethod (x));
			$s.$
		}
	}
}");
			Assert.IsNotNull (provider.Find ("Foo2"), "method 'Foo2' not found.");
		}
		
		/// <summary>
		/// Bug 676311 - auto completion too few proposals in fluent API (Moq)
		/// </summary>
		[Test()]
		public void TestBug676311_Case2 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"using System;
using System.Linq.Expressions;

namespace Test
{
	public interface IFoo<T>
	{
		void Foo1 ();
	}

	public interface IFoo<T, S>
	{
		void Foo2 ();
	}
	
	public class Test<T>
	{
		public IFoo<T> TestMe (Expression<Action<T>> act)
		{
			return null;
		}
		
		public IFoo<T, S> TestMe<S> (Expression<Func<S, T>> func)
		{
			return null;
		}
		
		public void TestMethod (string str)
		{
		}
	}
	
	class MainClass
	{
		public static void Main (string[] args)
		{
			var t = new Test<string> ();
			var s = t.TestMe (x => t.TestMethod (x));
			$s.$
		}
	}
}");
			Assert.IsNotNull (provider.Find ("Foo1"), "method 'Foo2' not found.");
		}
		
		/// <summary>
		/// Bug 678340 - Cannot infer types from Dictionary<K,V>.Values
		/// </summary>
		[Test()]
		public void TestBug678340 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"using System;
using System.Collections.Generic;

public class Test
{
	public void SomeMethod ()
	{
		var foo = new Dictionary<string,Test> ();
		foreach (var bar in foo.Values) {
			$bar.$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("SomeMethod"), "method 'SomeMethod' not found.");
		}
		/// <summary>
		/// Bug 678340 - Cannot infer types from Dictionary<K,V>.Values
		/// </summary>
		[Test()]
		public void TestBug678340_Case2 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"public class Foo<T>
{
	public class TestFoo
	{
		public T Return ()
		{
			
		}
	}
	
	public TestFoo Bar = new TestFoo ();
}

public class Test
{
	public void SomeMethod ()
	{
		Foo<Test> foo = new Foo<Test> ();
		var f = foo.Bar;
		$f.Return ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("SomeMethod"), "method 'SomeMethod' not found.");
		}
		
		/// <summary>
		/// Bug 679792 - MonoDevelop becomes unresponsive and leaks memory
		/// </summary>
		[Test()]
		public void TestBug679792 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"using System.Collections.Generic;

class TestClass
{
	public static void Main (string[] args)
	{
		Dictionary<string, Dictionary<string, TestClass>> cache;
		$cache[""Hello""] [""World""] = new $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TestClass"), "class 'TestClass' not found.");
		}
		
		/// <summary>
		/// Bug 679995 - Variable missing from completiom
		/// </summary>
		/// 
		[Test()]
		public void TestBug679995 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"class TestClass
{
	public void Foo ()
	{
		using (var testMe = new TestClass ()) {
			$$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("testMe"), "variable 'testMe' not found.");
		}
		
		/// <summary>
		/// Bug 680264 - Lamba completion inference issues
		/// </summary>
		/// 
		[Test()]
		public void TestBug680264 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
public delegate S Func<T, S> (T t);

public static class Linq
{
	public static bool Any<T> (this T[] t, Func<T, bool> func)
	{
		return true;
	}
}

class TestClass
{
	public void Foo ()
	{
		TestClass[] test;
		$test.Any (t => t.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
		}
		
		/// <summary>
		/// Bug 683037 - Missing autocompletion when 'using' directive references namespace by relative names
		/// </summary>
		/// 
		[Test()]
		public void TestBug683037 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"namespace N1.N2
{
	public class C1
	{
		public void Foo () {
			System.Console.WriteLine (1);
		}
	}
}

namespace N1
{
	using N2;

	public class C2
	{
		public static void Main (string[] args)
		{
			C1 x = new C1 ();

			$x.$
		}
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
		}
		
		/// <summary>
		/// Bug 690606 - Incomplete subclasses listing in code completion


		/// <summary>
		/// Bug 690606 - Incomplete subclasses listing in code completion
		/// </summary>
		[Test()]
		public void TestBug690606 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
public abstract class Base {}
public abstract class MyBase<T> : Base {}
public class A : MyBase<string> {}
public class B : MyBase<int> {}
public class C : MyBase<bool> {}

public class Test
{
	public static void Main (string[] args)
	{
		$Base x = new $
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
			Assert.IsNotNull (provider.Find ("B"), "class 'B' not found.");
			Assert.IsNotNull (provider.Find ("C"), "class 'C' not found.");
		}
		
		/// <summary>
		/// Bug 1744 - [New Resolver] Issues while typing a property
		/// </summary>
		[Test()]
		public void Test1744 ()
		{
			var provider = CreateProvider (
@"
public class Test
{
	$public $
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("void"), "class 'void' not found.");
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found.");
			Assert.IsNotNull (provider.Find ("System"), "namespace 'System' not found.");
		}
		
		/// <summary>
		/// Bug 1747 - [New Resolver] Code completion issues when declaring a generic dictionary
		/// </summary>
		[Test()]
		public void Test1747()
		{
			var provider = CreateProvider(
@"using System.Collections.Generic;
public class Test
{
	$Dictionary<int,string> field = new $
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Dictionary<int, string>"), "type 'Dictionary<int, string>' not found.");
			Assert.AreEqual ("Dictionary<int, string>", provider.DefaultCompletionString);
		}
		
		[Test()]
		public void Test1747Case2 ()
		{
			var provider = CreateProvider (
@"public class Test
{
	$Dictionary<int, string> d$
}
");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider not empty.");
			
			provider = CreateCtrlSpaceProvider (
@"public class Test
{
	$Dictionary<int, string> $
}
");
			Assert.IsFalse (provider == null || provider.Count == 0, "provider not found.");
			
		}
		
		[Test()]
		public void TestCompletionInTryCatch ()
		{
			CompletionDataList provider = CreateProvider (
@"class Test { public void TM1 () {} public void TM2 () {} public int TF1; }
class CCTest {
void TestMethod ()
{
	Test t;
	try {
		$t.$
}
}
");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("TM1"));
			Assert.IsNotNull (provider.Find ("TM2"));
			Assert.IsNotNull (provider.Find ("TF1"));
		}
		
		[Test()]
		public void TestPartialCompletionData ()
		{
			var provider = CreateProvider (
@"
public partial class TestMe
{
	partial void MyMethod ();
	partial void Implemented ();
}

public partial class TestMe
{
	$partial $

	partial void Implemented () { }
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("MyMethod"), "method 'MyMethod' not found.");
			Assert.IsNull (provider.Find ("Implemented"), "method 'Implemented'  found.");
		}
		
		/// <summary>
		/// Bug 224 - Code completion cannot handle lambdas properly. 
		/// </summary>
		[Test()]
		public void TestBug224 ()
		{
			CombinedProviderTest (
@"
using System;

public sealed class CrashEventArgs : EventArgs
{
	public int ArgsNum { get; set; }

	public CrashEventArgs ()
	{
		
	}
}

interface ICrashMonitor
{
	event EventHandler<CrashEventArgs> CrashDetected;

	void StartMonitoring ();

	void StopMonitoring ();
}

namespace ConsoleProject
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			ICrashMonitor mon;
			$mon.CrashDetected += (sender, e) => e.$
		}
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("ArgsNum"), "property 'ArgsNum' not found.");
			});
		}
		
		[Test()]
		public void TestParameterContext ()
		{
			var provider = CreateProvider (
@"
public class TestMe
{
	$void TestMe (TestClassParameter t$
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider was not empty.");
		}
		
		/// <summary>
		/// Bug 2123 - Completion kicks in after an array type is used in method parameters
		/// </summary>
		[Test()]
		public void TestParameterContextCase2FromBug2123 ()
		{
			CompletionDataList provider = CreateProvider (
@"class Program
{
	public Program ($string[] a$)
	{
	}
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestParameterContextNameProposal ()
		{
			var provider = CreateCtrlSpaceProvider (
@"
public class TestMe
{
	$void TestMe (TestClassParameter t$
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("testClassParameter"), "'testClassParameter' not found.");
			Assert.IsNotNull (provider.Find ("classParameter"), "'classParameter' not found.");
			Assert.IsNotNull (provider.Find ("parameter"), "'parameter' not found.");
		}
		
		[Test()]
		public void TestParameterTypeNameContext ()
		{
			CombinedProviderTest (
@"class Program
{
	public Program ($System.$)
	{
	}
}", provider => {
				Assert.IsNotNull (provider.Find ("Object"), "'Object' not found.");
			});
		}
		
		[Test()]
		public void TestMethodNameContext ()
		{
			CompletionDataList provider = CreateProvider (
@"using System;
namespace Test 
{
	class Program
	{
		void SomeMethod ()
		{
			
		}
		
		$public void T$
	}
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestNamedParameters ()
		{
			CombinedProviderTest (
@"class MyClass {
    string Bar { get; set; }

    void MethodOne(string foo="""", string bar="""")
	{
    }

    void MethodTwo() {
        $MethodOne(b$
    }
}", provider => {
				Assert.IsNotNull (provider.Find ("bar:"), "'bar:' not found.");
				Assert.IsNotNull (provider.Find ("foo:"), "'foo:' not found.");
			});
		}
		[Test()]
		public void TestNamedParameters2 ()
		{
			var provider = CreateCtrlSpaceProvider (
@"class MyClass {
    string Bar { get; set; }

    void MethodOne(string foo="""", string bar="""")
	{
    }

    void MethodTwo() {
        MethodOne($$);
    }
}");
			Assert.IsNotNull (provider.Find ("bar:"), "'bar:' not found.");
			Assert.IsNotNull (provider.Find ("foo:"), "'foo:' not found.");
		}

		[Test()]
		public void TestNamedParametersConstructorCase ()
		{
			CombinedProviderTest (
@"class MyClass {
    MyClass(string foo="""", string bar="""")
	{
    }

    void MethodTwo() {
        $new MyClass(b$
    }
}", provider => {
				Assert.IsNotNull (provider.Find ("bar"), "'bar' not found.");
				Assert.IsNotNull (provider.Find ("foo"), "'foo' not found.");
			});
		}
		
		[Test()]
		public void TestConstructorThisBase ()
		{
			CombinedProviderTest (
@"class Program
{
	public Program () : $t$
	{
	}
}", provider => {
				Assert.IsNotNull (provider.Find ("this"), "'this' not found.");
				Assert.IsNotNull (provider.Find ("base"), "'base' not found.");
			});
		}
		
		[Test()]
		public void TestAnonymousArguments ()
		{
			CombinedProviderTest (
@"
using System;
class Program
{
	public static void Main ()
	{
		EventHandler f = delegate (object sender, EventArgs args) {
			$Console.WriteLine(s$
		};
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("sender"), "'sender' not found.");
				Assert.IsNotNull (provider.Find ("args"), "'args' not found.");
			});
		}
		
		[Test()]
		public void TestCodeCompletionCategorySorting ()
		{
			CompletionDataList provider = CreateProvider (
@"class CClass : BClass
{
	public int C;
}

class BClass : AClass
{
	public int B;
}

class AClass
{
	public int A;
}

class Test
{
	public void TestMethod ()
	{
		CClass a;
		$a.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			
			var list = new List<CompletionCategory> ();
			
			for (int i = 0; i < provider.Count; i++) {
				if (list.Contains (provider [i].CompletionCategory))
					continue;
				list.Add (provider [i].CompletionCategory);
			}	
			Assert.AreEqual (4, list.Count);
			
			list.Sort ();
			Assert.AreEqual ("CClass", list [0].DisplayText);
			Assert.AreEqual ("BClass", list [1].DisplayText);
			Assert.AreEqual ("AClass", list [2].DisplayText);
			Assert.AreEqual ("System.Object", list [3].DisplayText);
		}
		
		[Test()]
		public void TestAsExpressionContext ()
		{
			var provider = CreateProvider (
@"class CClass : BClass
{
	public int C;
}

class BClass : AClass
{
	public int B;
}

class AClass
{
	public int A;
}

class Test
{
	public void TestMethod ()
	{
		AClass a;
		$a as A$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("AClass"), "'AClass' not found.");
			Assert.IsNotNull (provider.Find ("BClass"), "'BClass' not found.");
			Assert.IsNotNull (provider.Find ("CClass"), "'CClass' not found.");
			Assert.IsNotNull (provider.Find ("Test"), "'Test' not found.");
			Assert.IsNull (provider.Find ("TestMethod"), "'TestMethod' found.");
			
		}
		
		/// <summary>
		/// Bug 2109 - [Regression] Incorrect autocompletion when declaring an enum 
		/// </summary>
		[Test()]
		public void TestBug2109B ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace Foobar
{
    class MainClass
    {
        public enum Foo
        {
            Value1,
            Value2
        }

        public class Test
        {
            Foo Foo {
                get; set;
            }

            public static void Method (Foo foo)
            {
                $Foo.$
            }
        }
    }
}
");
			Assert.AreEqual (2, provider.Count); // 2 fields
			Assert.IsNotNull (provider.Find ("Value1"), "field 'Value1' not found.");
			Assert.IsNotNull (provider.Find ("Value2"), "field 'Value2' not found.");
		}
		
		/// <summary>
		/// Bug 3581 - [New Resolver] No code completion on Attributes
		/// </summary>
		[Test()]
		public void TestBug3581 ()
		{
			CompletionDataList provider = CreateProvider (
@"using System;

namespace Foobar
{
	class Intent 
	{
		public static int Foo = 0;
		public static int Bar = 1;
	}
	
	class MyAttribute : Attribute
	{
		public int[] Categories;
	}
	
	[MyAttribute(Categories = new [] { $I$ })]
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine (ddsd);
		}
	}
}

");
			Assert.IsNotNull (provider.Find ("Intent"), "'Intent' not found.");
		}
		
		[Test()]
		public void TestForConditionContext ()
		{
			CompletionDataList provider = CreateProvider (
@"using System;

class MainClass
{
	public static void Main (string[] args)
	{
		$for (int i = 0; i < System.$
	}
}
");
			Assert.IsNotNull (provider.Find ("Math"), "'Math' not found.");
		}
		
		[Ignore("Mcs bug")]
		[Test()]
		public void TestConditionalExpression ()
		{
			CompletionDataList provider = CreateProvider (
@"using System;

class MainClass
{
	public static void Main (string[] args)
	{
		int a;
		$a = true ? System.$
	}
}
");
			Assert.IsNotNull (provider.Find ("Math"), "'Math' not found.");
		}
		
		/// <summary>
		/// Bug 3655 - Autocompletion does not work for the assembly attribute [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyExternalAssembly")] 
		/// </summary>
		[Test()]
		public void Test3655 ()
		{
			CombinedProviderTest (@"$[a$", provider => {
				Assert.IsNotNull (provider.Find ("assembly"), "'assembly' not found.");
				Assert.IsNotNull (provider.Find ("System"), "'System' not found.");
			});
		}
		
		[Test()]
		public void Test3655Case2 ()
		{
			CombinedProviderTest (@"$[assembly:System.R$", provider => {
				Assert.IsNotNull (provider.Find ("Runtime"), "'Runtime' not found.");
			});
		}
		
		[Test()]
		public void Test3655Case2Part2 ()
		{
			CombinedProviderTest (@"$[assembly:System.$", provider => {
				Assert.IsNotNull (provider.Find ("Runtime"), "'Runtime' not found.");
			});
		}
		
		[Test()]
		public void Test3655Case3 ()
		{
			CombinedProviderTest (@"$[assembly:System.Runtime.C$", provider => {
				Assert.IsNotNull (provider.Find ("CompilerServices"), "'CompilerServices' not found.");
			});
		}
		
		[Test()]
		public void Test3655Case3Part2 ()
		{
			CombinedProviderTest (@"$[assembly:System.Runtime.$", provider => {
				Assert.IsNotNull (provider.Find ("CompilerServices"), "'CompilerServices' not found.");
			});
		}
		
		[Test()]
		public void Test3655Case4 ()
		{
			CombinedProviderTest (@"$[assembly:System.Runtime.CompilerServices.I$", provider => {
				Assert.IsNotNull (provider.Find ("InternalsVisibleTo"), "'InternalsVisibleTo' not found.");
			});
		}
		
		[Test()]
		public void Test3655Case4Part2 ()
		{
			CombinedProviderTest (@"$[assembly:System.Runtime.CompilerServices.$", provider => {
				Assert.IsNotNull (provider.Find ("InternalsVisibleTo"), "'InternalsVisibleTo' not found.");
			});
		}
		
		[Test()]
		public void TestUsingContext ()
		{
			CombinedProviderTest (@"$using System.$", provider => {
				Assert.IsNotNull (provider.Find ("IO"), "'IO' not found.");
				Assert.IsNull (provider.Find ("Console"), "'Console' found.");
			});
		}
		
		[Test()]
		public void TestUsingContextCase2 ()
		{
			CombinedProviderTest (@"$using System.U$", provider => {
				Assert.IsNotNull (provider.Find ("IO"), "'IO' not found.");
				Assert.IsNull (provider.Find ("Console"), "'Console' found.");
			});
		}

		[Test()]
		public void TestInterfaceReturnType()
		{
			var provider = CreateProvider(
@"using System;
using System.Collections.Generic;

class MainClass
{
	public IEnumerable<string> Test ()
	{
		$return new a$
	}
}
");
			Assert.IsNotNull(provider.Find("string[]"), "'string[]' not found.");
			Assert.IsNotNull(provider.Find("List<string>"), "'List<string>' not found.");
			Assert.IsNull(provider.Find("IEnumerable"), "'IEnumerable' found.");
			Assert.IsNull(provider.Find("IEnumerable<string>"), "'IEnumerable<string>' found.");
		}

		[Test()]
		public void TestInterfaceReturnTypeCase2 ()
		{
			var provider = CreateProvider (
@"using System;
using System.Collections.Generic;

class MainClass
{
	public IEnumerable<string> Test ()
	{
		$return new System.Collections.Generic.a$
	}
}
");
			Assert.IsNotNull (provider.Find ("List"), "'List' not found.");
			Assert.IsNull (provider.Find ("IEnumerable"), "'IEnumerable' found.");
		}

		[Test()]
		public void TestInterfaceReturnTypeCase3 ()
		{
			var provider = CreateProvider (
@"using System;
using System.Collections.Generic;

class MainClass
{
	public IEnumerable<string> Test ()
	{
		$return new System.Collections.Generic.$
	}
}
");
			Assert.IsNotNull (provider.Find ("List"), "'List' not found.");
			Assert.IsNull (provider.Find ("IEnumerable"), "'IEnumerable' found.");
		}


		/// <summary>
		/// Bug 3957 - [New Resolver]Override completion doesn't work well for overloaded methods
		/// </summary>
		[Test()]
		public void TestBug3957 ()
		{
			var provider = CreateProvider (
@"class A
{
    public virtual void Method()
    {}
    public virtual void Method(int i)
    {}
}

class B : A
{
	$override $
}

");
			Assert.AreEqual (2, provider.Data.Where (d => d.DisplayText == "Method").Count ());
		}

		/// <summary>
		/// Bug 3973 - code completion forgets context if text is deleted 
		/// </summary>
		[Test()]
		public void TestBug3973 ()
		{
			var provider = CreateProvider (
@"
using System;

class A
{
	public static void Main (string[] args)
	{
		Console.$W$
	}
}

");
			Assert.IsNotNull (provider.Find ("WriteLine"), "'WriteLine' not found.");
		}

		/// <summary>
		/// Bug 4017 - code completion in foreach does not work for local variables declared in the same block
		/// </summary>
		[Test()]
		public void TestBug4017()
		{
			var provider = CreateProvider (
@"
class TestClass
{
    void Foo()
    {
        string[] args = null;
        $foreach(string arg in a$
    }
}
");
			Assert.IsNotNull (provider.Find ("args"), "'args' not found.");
		}

		/// <summary>
		/// Bug 4020 - code completion handles explicit interface implementations improperly
		/// </summary>
		[Test()]
		public void TestBug4020 ()
		{
			// todo: maybe a better solution would be 
			//       having an item to insert the proper cast on 'Dispose' ?
			var provider = CreateProvider (
@"
using System;
namespace Test
{
    class TestClass : IDisposable
    {
        void IDisposable.Dispose ()
        {
        }
        public void Foo()
        {
            $D$
        }
    }
}
");
			Assert.IsNull (provider.Find ("Dispose"), "'Dispose' found.");
		}


		/// <summary>
		/// Bug 4085 - code completion problem with generic dictionary
		/// </summary>
		[Test()]
		public void TestBug4085()
		{
			// Name proposal feature breaks here
			var provider = CreateCtrlSpaceProvider(
@"using System.Collections.Generic;
namespace Test
{
	class TestClass
	{
		static void Main()
		{
			$IDictionary<string, TestClass> foo = new Dictionary<string, $
		}
	}
}

");
			Assert.IsNotNull(provider.Find("TestClass"), "'TestClass' not found.");
		}

		/// <summary>
		/// Bug 4283 - Newresolver: completing constructor parameters
		/// </summary>
		[Test()]
		public void TestBug4283()
		{
			var provider = CreateCtrlSpaceProvider(
@"class Program
{
	public Program (int test) : base($)
	{
	}
}");
			Assert.IsNotNull(provider.Find("test"), "'test' not found.");
		}

		[Test()]
		public void TestBug4283ThisCase()
		{
			var provider = CreateCtrlSpaceProvider(
@"class Program
{
	public Program (int test) : this($)
	{
	}
}");
			Assert.IsNotNull(provider.Find("test"), "'test' not found.");
		}

		/// <summary>
		/// Bug 4290 - Parameter completion exception inserting method with arguments before other methods
		/// </summary>
		[Test()]
		public void TestBug4290()
		{
			// just test for exception
			ParameterCompletionTests.CreateProvider (
@"using System;
namespace Test
{
    class TestClass  
    {
        $public static void Foo(string bar,$
        public static void Main(string[] args)
        {
        }
    }
}");
		}

		/// <summary>
		/// Bug 4174 - Intellisense popup after #region (same line) 
		/// </summary>
		[Test()]
		public void TestBug4174()
		{
			var provider = CreateProvider(
@"
namespace Test
{
	class TestClass  
    {
$#region S$
    }
}");
			Assert.IsTrue(provider == null || provider.Count == 0);
		}

		/// <summary>
		/// Bug 4323 - Parameter completion exception while attempting to instantiate unknown class
		/// </summary>
		[Test()]
		public void TestBug4323()
		{
			// just test for exception
			ParameterCompletionTests.CreateProvider(
@"namespace Test
{
    class TestClass
    {
        public static void Main(string[] args)
        {
            $object foo = new Foo($
        }
    }
}");
		}

		[Test()]
		public void TestParameterAttributeContext()
		{
			CombinedProviderTest(
@"using System;
using System.Runtime.InteropServices;

public class Test
{
	$static extern IntPtr somefunction([MarshalAs(UnmanagedType.LPTStr)] string fileName, [MarshalAs(UnmanagedType.$
}
", provider => {
				Assert.IsNotNull(provider.Find("LPStr"), "'LPStr' not found.");
			});
		}


		/// <summary>
		/// Bug 1051 - Code completion can't handle interface return types properly
		/// </summary>
		[Test()]
		public void TestBug1051()
		{
			CombinedProviderTest(
@"using System;
using System.Collections.Generic;

public class Test
{
	IEnumerable<string> TestFoo()
	{
		$return new $
	}
}
", provider => {
				Assert.IsNull(provider.Find("IEnumerable<string>"), "'IEnumerable<string>' found.");
				Assert.IsNotNull(provider.Find("List<string>"), "'List<string>' not found.");
				Assert.IsNotNull(provider.Find("string[]"), "'string[]' not found.");
			});
		}

		/// <summary>
		/// Bug 2668 - No completion offered for enum keys of Dictionaries 
		/// </summary>
		[Test()]
		public void TestBug2668()
		{
			CombinedProviderTest(
@"using System;
using System.Collections.Generic;

public enum SomeEnum { One, Two }

public class Test
{
	void TestFoo()
	{
		Dictionary<SomeEnum,int> dict = new Dictionary<SomeEnum,int>();
		$dict[O$

	}
}
", provider => {
				Assert.IsNotNull(provider.Find("SomeEnum"), "'SomeEnum' not found.");
				Assert.IsNotNull(provider.Find("SomeEnum.One"), "'SomeEnum.One' not found.");
			});
		}

		/// <summary>
		/// Bug 4487 - Filtering possible types for new expressions a bit too aggressively
		/// </summary>
		[Test()]
		public void TestBug4487()
		{
			// note 'string bar = new Test ().ToString ()' would be valid.
			CombinedProviderTest(
@"public class Test
{
	void TestFoo()
	{
		$string bar = new T$
	}
}
", provider => {
				Assert.IsNotNull(provider.Find("Test"), "'Test' not found.");
			});
		}

		/// <summary>
		/// Bug 4525 - Unexpected code completion exception
		/// </summary>
		[Test()]
		public void TestBug4525()
		{
			CombinedProviderTest(
@"public class Test
{
	$public new s$
}
", provider => {
				Assert.IsNotNull(provider.Find("static"), "'static' not found.");
			});
		}
		/// <summary>
		/// Bug 4604 - [Resolver] Attribute Properties are not offered valid autocomplete choices
		/// </summary>
		[Test()]
		public void TestBug4604()
		{
			CombinedProviderTest(
@"
		public sealed class MyAttribute : System.Attribute
		{
			public bool SomeBool {
				get;
				set;
			}
		}
$[MyAttribute(SomeBool=t$
public class Test
{
}
", provider => {
				Assert.IsNotNull(provider.Find("true"), "'true' not found.");
				Assert.IsNotNull(provider.Find("false"), "'false' not found.");
			});
		}


		/// <summary>
		/// Bug 4624 - [AutoComplete] Attribute autocomplete inserts entire attribute class name. 
		/// </summary>
		[Ignore("MCS BUG")]
		[Test()]
		public void TestBug4624()
		{
			CombinedProviderTest(
@"using System;

enum TestEnum
{
   $[E$
   EnumMember
}

", provider => {
				Assert.IsNotNull(provider.Find("Obsolete"), "'Obsolete' not found.");
			});
		}

		[Test()]
		public void TestCatchContext()
		{
			CombinedProviderTest(
@"using System;

class Foo
{
	void Test ()
	{
		$try { } catch (S$
	}
}


", provider => {
				Assert.IsNotNull(provider.Find("Exception"), "'Exception' not found.");
				Assert.IsNull(provider.Find("String"), "'String' found.");
			});
		}

		[Test()]
		public void TestCatchContextFollowUp()
		{
			CombinedProviderTest(
@"using System;

class Foo
{
	void Test ()
	{
		$try { } catch (System.$
	}
}


", provider => {
				Assert.IsNotNull(provider.Find("Exception"), "'Exception' not found.");
				Assert.IsNull(provider.Find("String"), "'String' found.");
			});
		}

		/// <summary>
		/// Bug 4688 - No code completion in nested using statements
		/// </summary>
		[Test()]
		public void TestBug4688()
		{
			CombinedProviderTest(
@"using System;

public class TestFoo
{
	void Bar ()
	{
		// Read the file from
		$using (S$
	}
}

", provider => {
				Assert.IsNotNull(provider.Find("String"), "'String'not found.");
			});
		}

		/// <summary>
		/// Bug 4808 - Enums have an unknown 'split_char' member included in them.
		/// </summary>
		[Test()]
		public void TestBug4808()
		{
			var provider = CreateProvider(
@"using System;

enum Foo { A, B }
public class TestFoo
{
	void Bar ()
	{
		$Foo.$
	}
}
"
			);
			Assert.IsNotNull(provider.Find("A"));
			Assert.IsNotNull(provider.Find("B"));
			Assert.IsNull(provider.Find("split_char"), "'split_char' found.");
		}


		/// <summary>
		/// Bug 4961 - Code completion for enumerations in static classes doesn't work.
		/// </summary>
		[Test()]
		public void TestBug4961()
		{
			CombinedProviderTest(
				@"using System;
using System.Collections.Generic;

namespace EnumerationProblem
{
	public enum Options
	{
		GiveCompletion,
		IwouldLoveIt,
	}
	
	static class Converter
	{
		private static Dictionary<Options, string> options = new Dictionary<Options, string> () 
		{
			${ Options.$
		};
	}
}

", provider => {
				Assert.IsNotNull(provider.Find("GiveCompletion"));
				Assert.IsNotNull(provider.Find("IwouldLoveIt"));
			});
		}

		/// <summary>
		/// Bug 5191 - Creating extension method problem when typing "this" 
		/// </summary>
		[Test()]
		public void TestBug5191()
		{
			CombinedProviderTest(
@"using System;

static class Ext
{
	$public static void Foo(t$
}
", provider => {
				Assert.IsNotNull(provider.Find("this"), "'this' not found.");
			});

			CombinedProviderTest(
@"using System;

static class Ext
{
	$public static void Foo(int foo, t$
}
", provider => {
				Assert.IsNull(provider.Find("this"), "'this' found.");
			});
		}
		
		/// <summary>
		/// Bug 5404 - Completion and highlighting for pointers 
		/// </summary>
		[Test()]
		public void TestBug5404()
		{
			CombinedProviderTest(
				@"using System;

namespace TestConsole
{
unsafe class MainClass
{
public int i = 5, j =19;

public static void Main (string[] args)
{
MainClass*  mc;
$mc->$
}
}
}
", provider => {
				Assert.IsNotNull(provider.Find("i"), "'i' not found.");
			});
		}
		
		/// <summary>
		/// Bug 6146 - No intellisense on value keyword in property set method
		/// </summary>
		[Test()]
		public void TestBug6146()
		{
			CombinedProviderTest(
				@"using System;
public class FooBar
{
	public FooBar Foo {
		set {
			$value.$
		}
	}
}

", provider => {
				Assert.IsNotNull(provider.Find("Foo"));
			});
		}


		[Test()]
		public void TestBug6146Case2()
		{
			CombinedProviderTest(
				@"using System;
public class FooBar
{
	public FooBar Foo {
		set {
			$value.Foo.F$
		}
	}
}

", provider => {
				Assert.IsNotNull(provider.Find("Foo"));
			});
		}

		[Test()]
		public void TestCompletionInPreprocessorIf()
		{
			CombinedProviderTest(
				@"using System;
public class FooBar
{
	public static void Main (string[] args)
	{
		#if TEST
		$Console.$
		#endif
	}
}

", provider => {
				Assert.IsNotNull(provider.Find("WriteLine"));
			});
		}

		[Test()]
		public void TestCompletionInUndefinedPreprocessorIf()
		{
			CombinedProviderTest(
				@"using System;
public class FooBar
{
	public static void Main (string[] args)
	{
		#if UNDEFINED
		$Console.$
		#endif
	}
}

", provider => {
				Assert.IsNull(provider.Find("WriteLine"));
			});
		}

	}
}
