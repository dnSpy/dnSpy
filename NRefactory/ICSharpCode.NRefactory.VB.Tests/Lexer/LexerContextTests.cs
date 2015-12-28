// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Parser;

namespace ICSharpCode.NRefactory.VB.Tests.Lexer
{
	[TestFixture]
	public class LexerContextTests
	{
		[Test]
		public void SimpleGlobal()
		{
			RunTest(
				@"Option Explicit",
				@"enter Global
exit Global
"
			);
		}
		
		[Test]
		public void VariableWithXmlLiteral()
		{
			RunTest(
				@"Class Test
	Public Sub New()
		Dim x = <a />
	End Sub
End Class
",
				@"enter Global
	enter TypeDeclaration
		enter Identifier
		exit Identifier
		enter Member
			enter Identifier
			exit Identifier
			enter Body
				enter Identifier
				exit Identifier
				enter Expression
					enter Expression
						enter Expression
							enter Xml
								enter Xml
								exit Xml
							exit Xml
						exit Expression
					exit Expression
				exit Expression
			exit Body
		exit Member
	exit TypeDeclaration
exit Global
"
			);
		}
		
		[Test]
		public void MemberWithXmlLiteral()
		{
			RunTest(
				@"Class Test
	Private xml As XElement = <b />
	
	Public Sub New()
		Dim x = <a />
	End Sub
End Class
",
				@"enter Global
	enter TypeDeclaration
		enter Identifier
		exit Identifier
		enter Member
			enter Identifier
			exit Identifier
			enter Type
			exit Type
			enter Expression
				enter Expression
					enter Expression
						enter Xml
							enter Xml
							exit Xml
						exit Xml
					exit Expression
				exit Expression
			exit Expression
		exit Member
		enter Member
			enter Identifier
			exit Identifier
			enter Body
				enter Identifier
				exit Identifier
				enter Expression
					enter Expression
						enter Expression
							enter Xml
								enter Xml
								exit Xml
							exit Xml
						exit Expression
					exit Expression
				exit Expression
			exit Body
		exit Member
	exit TypeDeclaration
exit Global
"
			);
		}
		
		[Test]
		public void GlobalAttributeTest()
		{
			RunTest(
				@"<assembly: CLSCompliant(True)>
Class Test
	Public Sub New()
		Dim x = 5
	End Sub
End Class
",
				@"enter Global
	enter Attribute
	exit Attribute
	enter TypeDeclaration
		enter Identifier
		exit Identifier
		enter Member
			enter Identifier
			exit Identifier
			enter Body
				enter Identifier
				exit Identifier
				enter Expression
					enter Expression
						enter Expression
						exit Expression
					exit Expression
				exit Expression
			exit Body
		exit Member
	exit TypeDeclaration
exit Global
"
			);
		}
		
		[Test]
		public void ClassAttributeTest()
		{
			RunTest(
				@"<Serializable>
Class Test
	Public Sub New()
		Dim x = 5
	End Sub
End Class
",
				@"enter Global
	enter Attribute
	exit Attribute
	enter TypeDeclaration
		enter Identifier
		exit Identifier
		enter Member
			enter Identifier
			exit Identifier
			enter Body
				enter Identifier
				exit Identifier
				enter Expression
					enter Expression
						enter Expression
						exit Expression
					exit Expression
				exit Expression
			exit Body
		exit Member
	exit TypeDeclaration
exit Global
"
			);
		}
		
		[Test]
		public void MethodAttributeTest()
		{
			RunTest(
				@"Class Test
	<Test>
	Public Sub New()
		Dim x = 5
	End Sub
End Class
",
				@"enter Global
	enter TypeDeclaration
		enter Identifier
		exit Identifier
		enter Attribute
		exit Attribute
		enter Member
			enter Identifier
			exit Identifier
			enter Body
				enter Identifier
				exit Identifier
				enter Expression
					enter Expression
						enter Expression
						exit Expression
					exit Expression
				exit Expression
			exit Body
		exit Member
	exit TypeDeclaration
exit Global
"
			);
		}
		
		[Test]
		public void WithBlockTest()
		{
			RunTest(
				@"Class Test
	Public Sub New()
		With x
			
		End With
	End Sub
End Class
",
				@"enter Global
	enter TypeDeclaration
		enter Identifier
		exit Identifier
		enter Member
			enter Identifier
			exit Identifier
			enter Body
				enter Expression
					enter Expression
						enter Expression
						exit Expression
					exit Expression
				exit Expression
				enter Body
				exit Body
			exit Body
		exit Member
	exit TypeDeclaration
exit Global
"
			);
		}
		
		[Test]
		public void StatementsTest()
		{
			RunTest(
				@"Class Test
	Public Sub New()
		For i As Integer = 0 To 10
		
		Next
	
		For Each x As Integer In list
		
		Next
		
		Try
		
		Catch e As Exception
		
		End Try
	End Sub
End Class
",
				@"enter Global
	enter TypeDeclaration
		enter Identifier
		exit Identifier
		enter Member
			enter Identifier
			exit Identifier
			enter Body
				enter Identifier
					enter Expression
					exit Expression
				exit Identifier
				enter Type
				exit Type
				enter Expression
					enter Expression
						enter Expression
						exit Expression
					exit Expression
					enter Expression
						enter Expression
						exit Expression
					exit Expression
				exit Expression
				enter Body
				exit Body
				enter Identifier
					enter Expression
					exit Expression
				exit Identifier
				enter Type
				exit Type
				enter Expression
					enter Expression
						enter Expression
						exit Expression
					exit Expression
				exit Expression
				enter Body
				exit Body
				enter Body
				exit Body
				enter Identifier
				exit Identifier
				enter Type
				exit Type
				enter Body
				exit Body
			exit Body
		exit Member
	exit TypeDeclaration
exit Global
"
			);
		}
		
		[Test]
		public void ClassTest()
		{
			RunTest(
				@"Class MainClass ' a comment
	Dim under_score_field As Integer
	Sub SomeMethod()
		simple += 1
		For Each loopVarName In collection
		Next
	End Sub
End Class",
				@"enter Global
	enter TypeDeclaration
		enter Identifier
		exit Identifier
		enter Member
			enter Identifier
			exit Identifier
			enter Type
			exit Type
		exit Member
		enter Member
			enter Identifier
			exit Identifier
			enter Body
				enter Expression
					enter Expression
						enter Expression
						exit Expression
					exit Expression
					enter Expression
						enter Expression
						exit Expression
					exit Expression
				exit Expression
				enter Identifier
					enter Expression
					exit Expression
				exit Identifier
				enter Expression
					enter Expression
						enter Expression
						exit Expression
					exit Expression
				exit Expression
				enter Body
				exit Body
			exit Body
		exit Member
	exit TypeDeclaration
exit Global
");
		}
		
		[Test]
		public void CollectionInitializer()
		{
			RunTest(@"'
' Created by SharpDevelop.
' User: Siegfried
' Date: 22.06.2010
' Time: 21:29
'
' To change this template use Tools | Options | Coding | Edit Standard Headers.
'

Option Infer On

Imports System.Linq
Imports System.Xml.Linq

Module Program
	Sub Main()
		Console.WriteLine(""Hello World!"")
		
		Dim name = ""Test""
		Dim content = { 4, 5, New XAttribute(""a"", 3) }
		
		Dim xml = <<%= name %> <%= content %> />
		
		Console.ReadKey()
	End Sub
End Module",
			        @"enter Global
	enter Importable
	exit Importable
	enter Importable
	exit Importable
	enter TypeDeclaration
		enter Identifier
		exit Identifier
		enter Member
			enter Identifier
			exit Identifier
			enter Body
				enter Expression
					enter Expression
						enter Expression
						exit Expression
						enter Expression
							enter Expression
								enter Expression
									enter Expression
									exit Expression
								exit Expression
							exit Expression
						exit Expression
					exit Expression
				exit Expression
				enter Identifier
				exit Identifier
				enter Expression
					enter Expression
						enter Expression
						exit Expression
					exit Expression
				exit Expression
				enter Identifier
				exit Identifier
				enter Expression
					enter Expression
						enter Expression
							enter Expression
								enter Expression
								exit Expression
							exit Expression
						exit Expression
						enter Expression
							enter Expression
								enter Expression
								exit Expression
							exit Expression
						exit Expression
						enter Expression
							enter Expression
								enter ObjectCreation
									enter Expression
										enter Expression
											enter Expression
												enter Expression
												exit Expression
											exit Expression
										exit Expression
										enter Expression
											enter Expression
												enter Expression
												exit Expression
											exit Expression
										exit Expression
									exit Expression
								exit ObjectCreation
							exit Expression
						exit Expression
					exit Expression
				exit Expression
				enter Identifier
				exit Identifier
				enter Expression
					enter Expression
						enter Expression
							enter Xml
								enter Xml
									enter Expression
										enter Expression
											enter Expression
											exit Expression
										exit Expression
									exit Expression
									enter Expression
										enter Expression
											enter Expression
											exit Expression
										exit Expression
									exit Expression
								exit Xml
							exit Xml
						exit Expression
					exit Expression
				exit Expression
				enter Expression
					enter Expression
						enter Expression
						exit Expression
						enter Expression
						exit Expression
					exit Expression
				exit Expression
			exit Body
		exit Member
	exit TypeDeclaration
exit Global
");
		}
		
		[Test]
		public void Imports()
		{
			RunTest(@"Imports System
Imports System.Linq
Imports System.Collections.Generic",
			        @"enter Global
	enter Importable
	exit Importable
	enter Importable
	exit Importable
	enter Importable
	exit Importable
exit Global
");
		}
		
		void RunTest(string code, string expectedOutput)
		{
			ExpressionFinder p = new ExpressionFinder();
			VBLexer lexer = new VBLexer(new StringReader(code));
			Token t;
			
			do {
				t = lexer.NextToken();
				p.InformToken(t);
			} while (t.Kind != Tokens.EOF);
			
			Console.WriteLine(p.Output);
			
			Assert.IsEmpty(p.Errors);
			
			Assert.AreEqual(expectedOutput.Replace("\r", ""),
			                p.Output.Replace("\r", ""));
		}
	}
}
