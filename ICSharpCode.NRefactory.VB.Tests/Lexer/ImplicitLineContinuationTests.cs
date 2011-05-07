// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using ICSharpCode.NRefactory.VB.Parser;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Lexer
{
	[TestFixture]
	public class ImplicitLineContinuationTests
	{
		[Test]
		public void Example1()
		{
			string code = @"Module Test
    Sub Print(
        Param1 As Integer,
        Param2 As Integer)

        If (Param1 < Param2) Or
           (Param1 > Param2) Then
            Console.WriteLine(""Not equal"")
        End If
    End Sub
End Module";
			
			VBLexer lexer = GenerateLexer(new StringReader(code));
			
			CheckTokens(lexer, Tokens.Module, Tokens.Identifier, Tokens.EOL,
			            Tokens.Sub, Tokens.Identifier, Tokens.OpenParenthesis,
			            Tokens.Identifier, Tokens.As, Tokens.Integer, Tokens.Comma,
			            Tokens.Identifier, Tokens.As, Tokens.Integer, Tokens.CloseParenthesis, Tokens.EOL,
			            Tokens.If, Tokens.OpenParenthesis, Tokens.Identifier, Tokens.LessThan, Tokens.Identifier, Tokens.CloseParenthesis, Tokens.Or,
			            Tokens.OpenParenthesis, Tokens.Identifier, Tokens.GreaterThan, Tokens.Identifier, Tokens.CloseParenthesis, Tokens.Then, Tokens.EOL,
			            Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.OpenParenthesis, Tokens.LiteralString, Tokens.CloseParenthesis, Tokens.EOL,
			            Tokens.End, Tokens.If, Tokens.EOL,
			            Tokens.End, Tokens.Sub, Tokens.EOL,
			            Tokens.End, Tokens.Module);
		}
		
		[Test]
		public void QualifierInWith()
		{
			string code = @"Module Test
	Sub Print
		With xml
			Dim a = b.
				d
			Dim c = .
				Count
		End With
	End Sub
End Module";
			
			VBLexer lexer = GenerateLexer(new StringReader(code));
			
			CheckTokens(lexer, Tokens.Module, Tokens.Identifier, Tokens.EOL,
			            Tokens.Sub, Tokens.Identifier, Tokens.EOL,
			            Tokens.With, Tokens.Identifier, Tokens.EOL,
			            Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.EOL,
			            Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.Dot, Tokens.EOL,
			            Tokens.Identifier, Tokens.EOL,
			            Tokens.End, Tokens.With, Tokens.EOL,
			            Tokens.End, Tokens.Sub, Tokens.EOL,
			            Tokens.End, Tokens.Module);
		}
		
		[Test]
		public void Example2()
		{
			string code = @"Module Test
	Sub Print
		Dim a = _
			
			y
	End Sub
End Module";
			
			VBLexer lexer = GenerateLexer(new StringReader(code));
			
			CheckTokens(lexer, Tokens.Module, Tokens.Identifier, Tokens.EOL,
			            Tokens.Sub, Tokens.Identifier, Tokens.EOL,
			            Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.EOL, Tokens.Identifier, Tokens.EOL,
			            Tokens.End, Tokens.Sub, Tokens.EOL,
			            Tokens.End, Tokens.Module);
		}
		
		[Test]
		public void Query()
		{
			string code = @"Module Test
	Sub A
		Dim q = From x In a
			Select x
	End Sub
End Module";
			
			VBLexer lexer = GenerateLexer(new StringReader(code));
			
			CheckTokens(lexer, Tokens.Module, Tokens.Identifier, Tokens.EOL,
			            Tokens.Sub, Tokens.Identifier, Tokens.EOL,
			            Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.From, Tokens.Identifier, Tokens.In, Tokens.Identifier,
			            Tokens.Select, Tokens.Identifier, Tokens.EOL,
			            Tokens.End, Tokens.Sub, Tokens.EOL,
			            Tokens.End, Tokens.Module);
		}
		
		[Test]
		public void Query1()
		{
			string code = @"Module Test
	Sub A
		Dim actions = From a in b Select Sub()
									    Dim i = 1
									    Select Case i
									    End Select
									End Sub
	End Sub
End Module";
			
			VBLexer lexer = GenerateLexer(new StringReader(code));
			
			CheckTokens(lexer, Tokens.Module, Tokens.Identifier, Tokens.EOL,
			            Tokens.Sub, Tokens.Identifier, Tokens.EOL,
			            Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.From, Tokens.Identifier, Tokens.In, Tokens.Identifier, Tokens.Select, Tokens.Sub, Tokens.OpenParenthesis, Tokens.CloseParenthesis, Tokens.EOL,
			            Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.LiteralInteger, Tokens.EOL,
			            Tokens.Select, Tokens.Case, Tokens.Identifier, Tokens.EOL,
			            Tokens.End, Tokens.Select, Tokens.EOL,
			            Tokens.End, Tokens.Sub, Tokens.EOL,
			            Tokens.End, Tokens.Sub, Tokens.EOL,
			            Tokens.End, Tokens.Module);
		}
		
		/// <remarks>tests http://community.sharpdevelop.net/forums/p/12068/32893.aspx#32893</remarks>
		[Test]
		public void Bug_Thread12068()
		{
			string code = @"Class MainClass
  Public Shared Sub Main()
	Dim categoryNames = From p In AList  _
           Select p.AFunction(1,2,3) _
           Distinct
  End Sub
End Class";
			
			VBLexer lexer = GenerateLexer(new StringReader(code));
			
			CheckTokens(
				lexer, Tokens.Class, Tokens.Identifier, Tokens.EOL,
				Tokens.Public, Tokens.Shared, Tokens.Sub, Tokens.Identifier, Tokens.OpenParenthesis, Tokens.CloseParenthesis, Tokens.EOL,
				Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.From, Tokens.Identifier, Tokens.In, Tokens.Identifier,
				Tokens.Select, Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.OpenParenthesis, Tokens.LiteralInteger,
				Tokens.Comma, Tokens.LiteralInteger, Tokens.Comma, Tokens.LiteralInteger, Tokens.CloseParenthesis,
				Tokens.Distinct, Tokens.EOL,
				Tokens.End, Tokens.Sub, Tokens.EOL,
				Tokens.End, Tokens.Class
			);
		}
		
		[Test]
		public void LineContinuationAfterAttributes()
		{
			string code = @"<TestFixture>
Public Class TestContinuation
    <Test>
    Public Sub TestMethod
        Assert.Fail
    End Sub
    
    <Test> _
    Public Sub TestMethod2
        Assert.Fail
    End Sub
End Class";
			
			VBLexer lexer = GenerateLexer(new StringReader(code));
			
			CheckTokens(
				lexer, Tokens.LessThan, Tokens.Identifier, Tokens.GreaterThan,
				Tokens.Public, Tokens.Class, Tokens.Identifier, Tokens.EOL,
				Tokens.LessThan, Tokens.Identifier, Tokens.GreaterThan,
				Tokens.Public, Tokens.Sub, Tokens.Identifier, Tokens.EOL,
				Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.EOL,
				Tokens.End, Tokens.Sub, Tokens.EOL,
				Tokens.LessThan, Tokens.Identifier, Tokens.GreaterThan,
				Tokens.Public, Tokens.Sub, Tokens.Identifier, Tokens.EOL,
				Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.EOL,
				Tokens.End, Tokens.Sub, Tokens.EOL,
				Tokens.End, Tokens.Class
			);
		}
		
		[Test]
		public void NoILCAfterGlobalAttributes()
		{
			string code = "<Assembly: AssemblyTitle(\"My.UnitTests\")>" + Environment.NewLine +
				"<Assembly: AssemblyDescription(\"\")>";
			
			VBLexer lexer = GenerateLexer(new StringReader(code));
			
			CheckTokens(
				lexer, Tokens.LessThan, Tokens.Assembly, Tokens.Colon,
				Tokens.Identifier, Tokens.OpenParenthesis, Tokens.LiteralString,
				Tokens.CloseParenthesis, Tokens.GreaterThan, Tokens.EOL,
				Tokens.LessThan, Tokens.Assembly, Tokens.Colon,
				Tokens.Identifier, Tokens.OpenParenthesis, Tokens.LiteralString,
				Tokens.CloseParenthesis, Tokens.GreaterThan
			);
		}
		
		#region Helpers
		VBLexer GenerateLexer(StringReader sr)
		{
			return new VBLexer(sr);
		}
		
		void CheckTokens(VBLexer lexer, params int[] tokens)
		{
			for (int i = 0; i < tokens.Length; i++) {
				int token = tokens[i];
				Token t = lexer.NextToken();
				int next = t.Kind;
				Assert.AreEqual(token, next, "{2} of {3}: {0} != {1}; at {4}", Tokens.GetTokenString(token), Tokens.GetTokenString(next), i + 1, tokens.Length, t.Location);
			}
		}
		#endregion
	}
}
