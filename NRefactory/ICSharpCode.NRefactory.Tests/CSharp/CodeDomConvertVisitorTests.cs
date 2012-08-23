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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using Microsoft.CSharp;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp
{
	[TestFixture]
	public class CodeDomConvertVisitorTests : ResolverTestBase
	{
		CodeDomConvertVisitor convertVisitor;
		CSharpUnresolvedFile unresolvedFile;
		
		public override void SetUp()
		{
			base.SetUp();
			unresolvedFile = new CSharpUnresolvedFile("test.cs");
			unresolvedFile.RootUsingScope.Usings.Add(MakeReference("System"));
			unresolvedFile.RootUsingScope.Usings.Add(MakeReference("System.Collections.Generic"));
			unresolvedFile.RootUsingScope.Usings.Add(MakeReference("System.Linq"));
			
			convertVisitor = new CodeDomConvertVisitor();
			convertVisitor.AllowSnippetNodes = false;
			convertVisitor.UseFullyQualifiedTypeNames = true;
		}
		
		#region Helper methods
		string ConvertHelper(AstNode node, Action<CSharpCodeProvider, CodeObject, TextWriter, CodeGeneratorOptions> action)
		{
			CSharpResolver resolver = new CSharpResolver(compilation);
			resolver = resolver.WithCurrentUsingScope(unresolvedFile.RootUsingScope.Resolve(compilation));
			resolver = resolver.WithCurrentTypeDefinition(compilation.FindType(KnownTypeCode.Object).GetDefinition());
			var codeObj = convertVisitor.Convert(node, new CSharpAstResolver(resolver, node));
			
			StringWriter writer = new StringWriter();
			writer.NewLine = " ";
			action(new CSharpCodeProvider(), codeObj, writer, new CodeGeneratorOptions { IndentString = " " });
			return Regex.Replace(writer.ToString(), @"\s+", " ").Trim();
		}
		
		string ConvertExpression(Expression expr)
		{
			return ConvertHelper(expr, (p,obj,w,opt) => p.GenerateCodeFromExpression((CodeExpression)obj, w, opt));
		}
		
		string ConvertExpression(string code)
		{
			CSharpParser parser = new CSharpParser();
			var expr = parser.ParseExpression(code);
			Assert.IsFalse(parser.HasErrors);
			return ConvertExpression(expr);
		}
		
		string ConvertStatement(Statement statement)
		{
			return ConvertHelper(statement, (p,obj,w,opt) => p.GenerateCodeFromStatement((CodeStatement)obj, w, opt));
		}
		
		string ConvertStatement(string code)
		{
			CSharpParser parser = new CSharpParser();
			var expr = parser.ParseStatements(code).Single();
			Assert.IsFalse(parser.HasErrors);
			return ConvertStatement(expr);
		}
		
		string ConvertMember(EntityDeclaration entity)
		{
			return ConvertHelper(entity, (p,obj,w,opt) => p.GenerateCodeFromMember((CodeTypeMember)obj, w, opt));
		}
		
		string ConvertMember(string code)
		{
			CSharpParser parser = new CSharpParser();
			var expr = parser.ParseTypeMembers(code).Single();
			Assert.IsFalse(parser.HasErrors);
			return ConvertMember(expr);
		}
		
		string ConvertTypeDeclaration(EntityDeclaration decl)
		{
			return ConvertHelper(decl, (p,obj,w,opt) => p.GenerateCodeFromType((CodeTypeDeclaration)obj, w, opt));
		}
		
		string ConvertTypeDeclaration(string code)
		{
			CSharpParser parser = new CSharpParser();
			var syntaxTree = parser.Parse(code, "program.cs");
			Assert.IsFalse(parser.HasErrors);
			return ConvertTypeDeclaration((EntityDeclaration)syntaxTree.Children.Single());
		}
		#endregion
		
		#region Type References
		[Test]
		public void MultidimensionalArrayTypeReference()
		{
			Assert.AreEqual("default(int[,][])", ConvertExpression("default(int[,][])"));
		}
		
		[Test]
		public void NestedTypeInGenericType()
		{
			Assert.AreEqual("default(System.Collections.Generic.List<string>.Enumerator)",
			                ConvertExpression("default(List<string>.Enumerator)"));
			convertVisitor.UseFullyQualifiedTypeNames = false;
			Assert.AreEqual("default(List<string>.Enumerator)",
			                ConvertExpression("default(List<string>.Enumerator)"));
		}
		#endregion
		
		#region Arrays
		[Test]
		public void CreateArray()
		{
			Assert.AreEqual("new int[10]", ConvertExpression("new int[10]"));
		}
		
		[Test, ExpectedException(typeof(NotSupportedException))]
		public void CreateJaggedArray()
		{
			ConvertExpression("new int[10][]");
		}
		
		[Test, ExpectedException(typeof(NotSupportedException))]
		public void Create2DArray()
		{
			ConvertExpression("new int[10, 20]");
		}
		
		[Test]
		public void CreateImplicitlyTypedArray()
		{
			// implicitly-typed array not supported in CodeDom, so the conversion should infer the type
			Assert.AreEqual("new int[] { 1, 2, 3}", ConvertExpression("new [] { 1, 2, 3 }"));
			Assert.AreEqual("new System.Collections.Generic.List<string>[] { new System.Collections.Generic.List<string>()}",
			                ConvertExpression("new [] { new List<string>() }"));
		}
		
		[Test, ExpectedException(typeof(NotSupportedException))]
		public void Create2DImplicitlyTypedArray()
		{
			ConvertExpression("new [,] { { 1, 2 }, { 3, 4 }}");
		}
		#endregion
		
		#region Operators
		[Test]
		public void ArithmeticOperators()
		{
			Assert.AreEqual("(0 + 1)", ConvertExpression("0 + 1"));
			Assert.AreEqual("(0 - 1)", ConvertExpression("0 - 1"));
			Assert.AreEqual("(0 * 1)", ConvertExpression("0 * 1"));
			Assert.AreEqual("(0 / 1)", ConvertExpression("0 / 1"));
			Assert.AreEqual("(0 % 1)", ConvertExpression("0 % 1"));
			Assert.AreEqual("(0 & 1)", ConvertExpression("0 & 1"));
			Assert.AreEqual("(0 | 1)", ConvertExpression("0 | 1"));
			Assert.AreEqual("(0 < 1)", ConvertExpression("0 < 1"));
			Assert.AreEqual("(0 > 1)", ConvertExpression("0 > 1"));
			Assert.AreEqual("(0 <= 1)", ConvertExpression("0 <= 1"));
			Assert.AreEqual("(0 >= 1)", ConvertExpression("0 >= 1"));
			Assert.AreEqual("(true && false)", ConvertExpression("true && false"));
			Assert.AreEqual("(true || false)", ConvertExpression("true || false"));
		}
		
		[Test]
		public void EqualityOperator()
		{
			Assert.AreEqual("(0 == 1)", ConvertExpression("0 == 1"));
			Assert.AreEqual("(default(object) == null)", ConvertExpression("default(object) == null"));
		}
		
		[Test]
		public void InEqualityOperator()
		{
			Assert.AreEqual("((0 == 1) == false)", ConvertExpression("0 != 1"));
			Assert.AreEqual("(default(object) != null)", ConvertExpression("default(object) != null"));
		}
		
		[Test]
		public void UnaryOperators()
		{
			Assert.AreEqual("(a == false)", ConvertExpression("!a"));
			Assert.AreEqual("(0 - a)", ConvertExpression("-a"));
			Assert.AreEqual("a", ConvertExpression("+a"));
		}
		
		[Test]
		public void Cast()
		{
			Assert.AreEqual("((double)(0))", ConvertExpression("(double)0"));
		}
		#endregion
		
		#region Member Access
		[Test]
		public void StaticProperty()
		{
			Assert.AreEqual("System.Environment.TickCount", ConvertExpression("Environment.TickCount"));
		}
		
		[Test]
		public void InstanceMethodInvocation()
		{
			Assert.AreEqual("this.Equals(null)", ConvertExpression("Equals(null)"));
		}
		
		[Test]
		public void StaticMethodInvocation()
		{
			Assert.AreEqual("object.Equals(null, null)", ConvertExpression("Equals(null, null)"));
		}
		
		[Test]
		public void BaseMemberAccess()
		{
			Assert.AreEqual("base.X", ConvertExpression("base.X"));
			Assert.AreEqual("base[i]", ConvertExpression("base[i]"));
		}
		
		[Test]
		public void GenericMethodReference()
		{
			Assert.AreEqual("this.Stuff<string>", ConvertExpression("this.Stuff<string>"));
			Assert.AreEqual("this.Stuff<string>", ConvertExpression("Stuff<string>"));
		}
		
		[Test]
		public void ByReferenceCall()
		{
			Assert.AreEqual("a.Call(ref x, out y, z)", ConvertExpression("a.Call(ref x, out y, z)"));
		}
		
		[Test]
		public void MemberAccessOnType()
		{
			Assert.AreEqual("string.Empty", ConvertExpression("string.Empty"));
		}
		#endregion
		
		#region Statements
		[Test]
		public void MethodInvocationStatement()
		{
			Assert.AreEqual("a.SomeMethod();", ConvertStatement("a.SomeMethod();"));
		}
		
		[Test]
		public void Assignment()
		{
			Assert.AreEqual("a = 1;", ConvertStatement("a = 1;"));
		}
		
		[Test, ExpectedException(typeof(NotSupportedException))]
		public void AssignmentNotSupportedInExpression()
		{
			ConvertStatement("a = b = 1;");
		}
		
		[Test]
		public void BlockStatement()
		{
			Assert.AreEqual("if (true) { a = 1; b = 2; }",
			                ConvertStatement("{ a = 1; b = 2; }"));
		}
		
		[Test]
		public void CompoundAssign()
		{
			Assert.AreEqual("a = (a + 1);", ConvertStatement("a += 1;"));
			Assert.AreEqual("a = (a - 1);", ConvertStatement("a -= 1;"));
			Assert.AreEqual("a = (a * 1);", ConvertStatement("a *= 1;"));
			Assert.AreEqual("a = (a / 1);", ConvertStatement("a /= 1;"));
			Assert.AreEqual("a = (a % 1);", ConvertStatement("a %= 1;"));
			Assert.AreEqual("a = (a & 1);", ConvertStatement("a &= 1;"));
			Assert.AreEqual("a = (a | 1);", ConvertStatement("a |= 1;"));
		}
		
		[Test]
		public void Increment()
		{
			Assert.AreEqual("a = (a + 1);", ConvertStatement("a++;"));
			Assert.AreEqual("a = (a + 1);", ConvertStatement("++a;"));
			Assert.AreEqual("a = (a - 1);", ConvertStatement("a--;"));
			Assert.AreEqual("a = (a - 1);", ConvertStatement("--a;"));
		}
		
		[Test]
		public void ForLoop()
		{
			Assert.AreEqual("for (int i = 0; (i < 10); i = (i + 1)) { }",
			                ConvertStatement("for (int i = 0; i < 10; i++) {}"));
		}
		
		[Test]
		public void WhileLoop()
		{
			Assert.AreEqual("for (new object(); (i < 10); new object()) { }",
			                ConvertStatement("while (i < 10);"));
		}
		
		[Test]
		public void VariableDeclarationWithArrayInitializer()
		{
			Assert.AreEqual("int[] nums = new int[] { 1, 2};",
			                ConvertStatement("int[] nums = { 1, 2 };"));
		}
		
		[Test]
		public void TryCatch()
		{
			Assert.AreEqual("try { a = 1; } catch (System.Exception ex) { ex.ToString(); }",
			                ConvertStatement("try { a = 1; } catch (Exception ex) { ex.ToString(); }"));
		}
		
		[Test]
		public void TryEmptyCatch()
		{
			Assert.AreEqual("try { a = 1; } catch (System.Exception ) { }",
			                ConvertStatement("try { a = 1; } catch (Exception) { }"));
		}
		
		[Test]
		public void TryFinally()
		{
			Assert.AreEqual("try { a = 1; } finally { a = 0; }",
			                ConvertStatement("try { a = 1; } finally { a = 0; }"));
		}
		#endregion
		
		#region Type Members
		[Test]
		public void MethodParameterNamedValue()
		{
			Assert.AreEqual("void M(string value) { System.Console.WriteLine(value); }",
			                ConvertMember("void M(string value) { Console.WriteLine(value); }"));
		}
		
		[Test]
		public void ValueInProperty()
		{
			Assert.AreEqual("string P { set { System.Console.WriteLine(value); } }",
			                ConvertMember("string P { set { Console.WriteLine(value); } }"));
		}
		
		[Test]
		public void MethodWithAttribute()
		{
			Assert.AreEqual("[Test()] void MethodWithAttribute() { }",
			                ConvertMember("[Test] void MethodWithAttribute() { }"));
		}
		
		[Test]
		public void PublicNonVirtualMethod()
		{
			Assert.AreEqual("public void Method() { }",
			                ConvertMember("public void Method() { }"));
		}
		
		[Test]
		public void PublicVirtualMethod()
		{
			Assert.AreEqual("public virtual void Method() { }",
			                ConvertMember("public virtual void Method() { }"));
		}
		
		[Test]
		public void NestedClass()
		{
			Assert.AreEqual("public class Outer { public class Inner { } }",
			                ConvertTypeDeclaration("class Outer { class Inner { } }"));
		}
		
		[Test]
		public void Constructor()
		{
			string code = "public class Test : Base { public Test(string x) : base(x) { } }";
			Assert.AreEqual(code, ConvertTypeDeclaration(code));
		}
		
		[Test]
		public void Enum()
		{
			string code = "public enum E { [Description(\"Text\")] None, B = 2, }";
			Assert.AreEqual(code, ConvertTypeDeclaration(code));
		}
		
		[Test]
		public void Field()
		{
			Assert.AreEqual("public class X {" +
			                " int A;" +
			                " int B; }",
			                ConvertMember("public class X { int A, B; }"));
		}
		
		[Test]
		public void Event()
		{
			Assert.AreEqual("public class X {" +
			                " protected event System.EventHandler A;" +
			                " protected event System.EventHandler B; }",
			                ConvertMember("public class X { protected event EventHandler A, B; }"));
		}
		#endregion
	}
}
