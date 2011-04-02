// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.PrettyPrinter;
using ICSharpCode.NRefactory.VB.Visitors;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.PrettyPrinter
{
	[TestFixture]
	public class VBNetOutputTest
	{
		void TestProgram(string program)
		{
			VBParser parser = ParserFactory.CreateParser(new StringReader(program));
			parser.Parse();
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			VBNetOutputVisitor outputVisitor = new VBNetOutputVisitor();
			outputVisitor.Options.OutputByValModifier = true;
			outputVisitor.VisitCompilationUnit(parser.CompilationUnit, null);
			Assert.AreEqual("", outputVisitor.Errors.ErrorOutput);
			Assert.AreEqual(StripWhitespace(program), StripWhitespace(outputVisitor.Text));
		}
		
		string StripWhitespace(string text)
		{
			text = text.Trim().Replace("\t", "").Replace("\r", "").Replace("\n", " ").Replace("  ", " ");
			while (text.Contains("  ")) {
				text = text.Replace("  ", " ");
			}
			return text;
		}
		
		void TestTypeMember(string program)
		{
			TestProgram("Class A\n" + program + "\nEnd Class");
		}
		
		void TestStatement(string statement)
		{
			TestTypeMember("Sub Method()\n" + statement + "\nEnd Sub");
		}
		
		void TestExpression(string expression)
		{
			VBParser parser = ParserFactory.CreateParser(new StringReader(expression));
			Expression e = parser.ParseExpression();
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			VBNetOutputVisitor outputVisitor = new VBNetOutputVisitor();
			e.AcceptVisitor(outputVisitor, null);
			Assert.AreEqual("", outputVisitor.Errors.ErrorOutput);
			Assert.AreEqual(StripWhitespace(expression), StripWhitespace(outputVisitor.Text));
		}
		
		[Test]
		public void Field()
		{
			TestTypeMember("Private a As Integer");
		}
		
		[Test]
		public void Method()
		{
			TestTypeMember("Sub Method()\nEnd Sub");
		}
		
		[Test]
		public void EnumWithBaseType()
		{
			TestProgram("Public Enum Foo As UShort\nEnd Enum");
		}
		
		[Test]
		public void PartialModifier()
		{
			TestProgram("Public Partial Class Foo\nEnd Class");
		}
		
		[Test]
		public void MustInheritClass()
		{
			TestProgram("Public MustInherit Class Foo\nEnd Class");
		}
		
		[Test]
		public void GenericClassDefinition()
		{
			TestProgram("Public Class Foo(Of T As {IDisposable, ICloneable})\nEnd Class");
		}
		
		[Test]
		public void GenericClassDefinitionWithBaseType()
		{
			TestProgram("Public Class Foo(Of T As IDisposable)\nInherits BaseType\nEnd Class");
		}
		
		[Test]
		public void GenericMethodDefinition()
		{
			TestTypeMember("Public Sub Foo(Of T As {IDisposable, ICloneable})(ByVal arg As T)\nEnd Sub");
		}
		
		[Test]
		public void ArrayRank()
		{
			TestStatement("Dim a As Object(,,)");
		}
		
		[Test]
		public void ArrayInitialization()
		{
			TestStatement("Dim a As Object() = New Object(10) {}");
			TestTypeMember("Private MultiDim As Integer(,) = {{1, 2}, {1, 3}}");
			TestExpression("New Integer(, ) {{1, 1}, {1, 1}}");
			TestTypeMember("Private _titles As String() = New String() {}");
		}
		
		[Test]
		public void MethodCallWithOptionalArguments()
		{
			TestExpression("M(, )");
		}
		
		[Test]
		public void IfStatement()
		{
			TestStatement("If a Then\n" +
			              "\tm1()\n" +
			              "ElseIf b Then\n" +
			              "\tm2()\n" +
			              "Else\n" +
			              "\tm3()\n" +
			              "End If");
		}
		
		[Test]
		public void ForNextLoop()
		{
			TestStatement("For i = 0 To 10\n" +
			              "Next");
			TestStatement("For i As Long = 10 To 0 Step -1\n" +
			              "Next");
		}
		
		[Test]
		public void DoLoop()
		{
			TestStatement("Do\n" +
			              "Loop");
			TestStatement("Do\n" +
			              "Loop While Not (i = 10)");
		}
		
		[Test]
		public void SelectCase()
		{
			TestStatement(@"Select Case i
	Case 0
	Case 1 To 4
	Case Else
End Select");
		}
		
		[Test]
		public void UsingStatement()
		{
			TestStatement(@"Using nf As New Font(), nf2 As New List(Of Font)(), nf3 = Nothing
	Bla(nf)
End Using");
		}
		
		[Test]
		public void UntypedVariable()
		{
			TestStatement("Dim x = 0");
		}
		
		[Test]
		public void UntypedField()
		{
			TestTypeMember("Dim x = 0");
		}
		
		[Test]
		public void Assignment()
		{
			TestExpression("a = b");
		}
		
		[Test]
		public void SpecialIdentifiers()
		{
			// Assembly, Ansi and Until are contextual keywords
			// Custom is valid inside methods, but not valid for field names
			TestExpression("Assembly = Ansi * [For] + Until - [Custom]");
		}
		
		[Test]
		public void DictionaryAccess()
		{
			TestExpression("c!key");
		}
		
		[Test]
		public void GenericMethodInvocation()
		{
			TestExpression("GenericMethod(Of T)(arg)");
		}
		
		[Test]
		public void SpecialIdentifierName()
		{
			TestExpression("[Class]");
		}
		
		[Test]
		public void GenericDelegate()
		{
			TestProgram("Public Delegate Function Predicate(Of T)(ByVal item As T) As String");
		}
		
		[Test]
		public void Enum()
		{
			TestProgram("Enum MyTest\nRed\n Green\n Blue\nYellow\n End Enum");
		}
		
		[Test]
		public void EnumWithInitializers()
		{
			TestProgram("Enum MyTest\nRed = 1\n Green = 2\n Blue = 4\n Yellow = 8\n End Enum");
		}
		
		[Test]
		public void SyncLock()
		{
			TestStatement("SyncLock a\nWork()\nEnd SyncLock");
		}
		
		[Test]
		public void Using()
		{
			TestStatement("Using a As New A()\na.Work()\nEnd Using");
		}
		
		[Test]
		public void Cast()
		{
			TestExpression("CType(a, T)");
		}
		
		[Test]
		public void DirectCast()
		{
			TestExpression("DirectCast(a, T)");
		}
		
		[Test]
		public void TryCast()
		{
			TestExpression("TryCast(a, T)");
		}
		
		[Test]
		public void PrimitiveCast()
		{
			TestExpression("CStr(a)");
		}
		
		[Test]
		public void TypeOfIs()
		{
			TestExpression("TypeOf a Is String");
		}
		
		[Test]
		public void PropertyWithAccessorAccessModifiers()
		{
			TestTypeMember("Public Property ExpectsValue() As Boolean\n" +
			               "\tPublic Get\n" +
			               "\tEnd Get\n" +
			               "\tProtected Set\n" +
			               "\tEnd Set\n" +
			               "End Property");
		}
		
		[Test]
		public void AutoProperty()
		{
			TestTypeMember("Public Property Value()");
			TestTypeMember("Public Property Value() As Integer");
			TestTypeMember("Public Property Value() As Integer = 5");
			TestTypeMember("Public Property Value() As New List()");
		}
		
		[Test]
		public void AbstractProperty()
		{
			TestTypeMember("Public MustOverride Property ExpectsValue() As Boolean");
			TestTypeMember("Public MustOverride ReadOnly Property ExpectsValue() As Boolean");
			TestTypeMember("Public MustOverride WriteOnly Property ExpectsValue() As Boolean");
		}
		
		[Test]
		public void AbstractMethod()
		{
			TestTypeMember("Public MustOverride Sub Run()");
			TestTypeMember("Public MustOverride Function Run() As Boolean");
		}
		
		[Test]
		public void InterfaceImplementingMethod()
		{
			TestTypeMember("Public Sub Run() Implements SomeInterface.Run\nEnd Sub");
			TestTypeMember("Public Function Run() As Boolean Implements SomeInterface.Bla\nEnd Function");
		}
		
		[Test]
		public void NamedAttributeArgument()
		{
			TestProgram("<Attribute(ArgName := \"value\")> _\n" +
			            "Class Test\n" +
			            "End Class");
		}
		
		[Test]
		public void ReturnTypeAttribute()
		{
			TestTypeMember("Function A() As <Attribute> String\n" +
			               "End Function");
		}
		
		[Test]
		public void AssemblyAttribute()
		{
			TestProgram("<Assembly: CLSCompliant>");
		}
		
		[Test]
		public void ModuleAttribute()
		{
			TestProgram("<Module: SuppressMessageAttribute>");
		}
		
		[Test]
		public void Interface()
		{
			TestProgram("Interface ITest\n" +
			            "Property GetterAndSetter() As Boolean\n" +
			            "ReadOnly Property GetterOnly() As Boolean\n" +
			            "WriteOnly Property SetterOnly() As Boolean\n" +
			            "Sub InterfaceMethod()\n" +
			            "Function InterfaceMethod2() As String\n" +
			            "End Interface");
		}
		
		[Test]
		public void OnErrorStatement()
		{
			TestStatement("On Error Resume Next");
		}
		
		[Test]
		public void OverloadedConversionOperators()
		{
			TestTypeMember("Public Shared Narrowing Operator CType(ByVal xmlNode As XmlNode) As TheBug\nEnd Operator");
			TestTypeMember("Public Shared Widening Operator CType(ByVal bugNode As TheBug) As XmlNode\nEnd Operator");
		}
		
		[Test]
		public void OverloadedTrueFalseOperators()
		{
			TestTypeMember("Public Shared Operator IsTrue(ByVal a As TheBug) As Boolean\nEnd Operator");
			TestTypeMember("Public Shared Operator IsFalse(ByVal a As TheBug) As Boolean\nEnd Operator");
		}
		
		[Test]
		public void OverloadedOperators()
		{
			TestTypeMember("Public Shared Operator +(ByVal bugNode As TheBug, ByVal bugNode2 As TheBug) As TheBug\nEnd Operator");
			TestTypeMember("Public Shared Operator >>(ByVal bugNode As TheBug, ByVal b As Integer) As TheBug\nEnd Operator");
		}
		
		[Test]
		public void AttributeOnParameter()
		{
			TestTypeMember("Sub Main(ByRef one As Integer, ByRef two As Integer, <Out> ByRef three As Integer)\nEnd Sub");
		}
		
		[Test]
		public void FieldWithoutType()
		{
			TestTypeMember("Dim X");
		}
		
		[Test]
		public void UsingStatementForExistingVariable()
		{
			TestStatement("Using obj\nEnd Using");
		}
		
		[Test]
		public void ContinueFor()
		{
			TestStatement("Continue For");
		}
		
		[Test]
		public void ForNextStatementWithFieldLoopVariable()
		{
			TestStatement("For Me.Field = 0 To 10\n" +
			              "Next Me.Field");
		}
		
		[Test]
		public void WithStatement()
		{
			TestStatement("With Ejes\n" +
			              "\t.AddLine(New Point(Me.ClientSize.Width / 2, 0), (New Point(Me.ClientSize.Width / 2, Me.ClientSize.Height)))\n" +
			              "End With");
		}
		
		[Test]
		public void NewConstraint()
		{
			TestProgram("Public Class Rational(Of T, O As {IRationalMath(Of T), New})\nEnd Class");
		}
		
		[Test]
		public void StructConstraint()
		{
			TestProgram("Public Class Rational(Of T, O As {IRationalMath(Of T), Structure})\nEnd Class");
		}
		
		[Test]
		public void ClassConstraint()
		{
			TestProgram("Public Class Rational(Of T, O As {IRationalMath(Of T), Class})\nEnd Class");
		}
		
		[Test]
		public void Integer()
		{
			TestExpression("16");
		}
		
		[Test]
		public void Double()
		{
			TestExpression("1.0");
		}
		
		[Test]
		public void HexadecimalInteger()
		{
			TestExpression("&H10");
		}
		
		[Test]
		public void HexadecimalMinusOne()
		{
			TestExpression("&Hffffffff");
		}
		
		[Test]
		public void TypeCharacters()
		{
			TestExpression("347S");
			TestExpression("347L");
			TestExpression("347D");
			TestExpression("347F");
			TestExpression("347US");
			TestExpression("347UI");
			TestExpression("347UL");
			TestExpression("\".\"C");
		}
		
		[Test]
		public void AddressOf()
		{
			TestExpression("AddressOf Abc");
		}
		
		[Test]
		public void ChainedConstructorCall()
		{
			TestExpression("MyBase.New()");
			TestExpression("Me.New()");
			TestExpression("MyClass.New()");
		}
		
		[Test]
		public void NewMethodCall()
		{
			TestExpression("something.[New]()");
		}
		
		[Test]
		public void ObjectInitializer()
		{
			TestExpression("New StringWriter() With { _\n" +
			               " .NewLine = Environment.NewLine, _\n" +
			               " .Encoding = Encoding.UTF8 _\n" +
			               "}");
		}
		
		[Test]
		public void EventDefinition()
		{
			TestTypeMember("Public Event MyEvent(ByVal sender As Object)");
		}
		
		[Test]
		public void Options()
		{
			TestProgram("Option Strict On\n" +
			            "Option Explicit On\n" +
			            "Option Infer On\n" +
			            "Option Compare Text");
		}
		
		[Test]
		public void UntypedForeach()
		{
			TestStatement("For Each x In myGuidArray\nNext");
		}
		
		[Test]
		public void MethodDefinitionWithOptionalParameter()
		{
			TestTypeMember("Sub M(Optional ByVal msg As String = Nothing, Optional ByRef output As String = Nothing)\nEnd Sub");
		}
		
		[Test]
		public void Module()
		{
			TestProgram("Module Test\n" +
			            " Sub M()\n" +
			            " End Sub\n" +
			            "End Module");
		}
		
		[Test]
		public void WithEvents()
		{
			TestTypeMember("Dim WithEvents a As Button");
		}
				
		[Test]
		public void FriendWithEventsField()
		{
			TestTypeMember("Friend WithEvents Button1 As System.Windows.Forms.Button");
		}
		
		[Test]
		public void SimpleFunctionLambda()
		{
			TestExpression("Function(x) x * x");
		}
		
		[Test]
		public void SimpleFunctionLambdaWithType()
		{
			TestExpression("Function(x As Integer) x * x");
		}
		
		[Test]
		public void SimpleSubLambdaWithType()
		{
			TestExpression("Sub(x As Integer) Console.WriteLine(x)");
		}
		
		[Test]
		public void BlockSubLambdaWithType()
		{
			TestExpression("Sub(x As Integer)\n" +
			               "	Console.WriteLine(x)\n" +
			               "End Sub");
		}
		
		[Test]
		public void BlockFunctionLambdaWithType()
		{
			TestExpression("Function(x As Integer) As Integer\n" +
			               "	If x < 2 Then\n" +
			               "		Return x\n" +
			               "	End If\n" +
			               "	Return x * x\n" +
			               "End Function");
		}
		
		[Test]
		public void XmlSimple()
		{
			TestExpression("<?xml?>\n" +
			               "<!-- test -->\n" +
			               "<Test>\n" +
			               "	<A />\n" +
			               "	<B test='a' <%= test %> />\n" +
			               "</Test>");
		}
		
		[Test]
		public void XmlNested()
		{
			TestExpression(@"<menu>
              <course name=""appetizer"">
                  <dish>Shrimp Cocktail</dish>
                  <dish>Escargot</dish>
              </course>
              <course name=""main"">
                  <dish>Filet Mignon</dish>
                  <dish>Garlic Potatoes</dish>
                  <dish>Broccoli</dish>
              </course>
              <course name=""dessert"">
                  <dish>Chocolate Cheesecake</dish>
              </course>
          </menu>");
		}
		
		[Test]
		public void XmlDocument()
		{
			TestExpression(@"<?xml version=""1.0""?>
                  <menu>
                      <course name=""appetizer"">
                          <dish>Shrimp Cocktail</dish>
                          <dish>Escargot</dish>
                      </course>
                  </menu>");
		}
		
		[Test]
		public void XmlNestedWithExpressions()
		{
			TestExpression(@"<?xml version=""1.0""?>
          <menu>
              <course name=""appetizer"">
                  <%= From m In menu _
                      Where m.Course = ""appetizer"" _
                      Select <dish><%= m.Food %></dish> %>
              </course>
              <course name=""main"">
                  <%= From m In menu _
                      Where m.Course = ""main"" _
                      Select <dish><%= m.Food %></dish> %>
              </course>
              <course name=""dessert"">
                  <%= From m In menu _
                      Where m.Course = ""dessert"" _
                      Select <dish><%= m.Food %></dish> %>
              </course>
          </menu>");
		}
		
		[Test]
		public void XmlAccessExpressions()
		{
			TestExpression("xml.<menu>.<course>");
			TestExpression("xml...<course>");
			TestExpression("xml...<course>(2)");
			TestExpression("item.@name");
		}
	}
}
