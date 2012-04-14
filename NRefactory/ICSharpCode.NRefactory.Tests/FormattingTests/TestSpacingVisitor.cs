// 
// TestFormattingVisitor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.CSharp.FormattingTests
{
	[TestFixture()]
	public class TestSpacingVisitor : TestBase
	{
		[Test()]
		public void TestFieldSpacesBeforeComma1()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			policy.SpaceBeforeFieldDeclarationComma = false;
			policy.SpaceAfterFieldDeclarationComma = false;
			
			Test(policy, @"class Test {
	int a           ,                   b,          c;
}",
@"class Test {
	int a,b,c;
}");
		}

		[Test()]
		public void TestFieldSpacesBeforeComma2 ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			policy.SpaceBeforeFieldDeclarationComma = true;
			policy.SpaceAfterFieldDeclarationComma = true;
			
			Test (policy, @"class Test {
	int a           ,                   b,          c;
}",
@"class Test {
	int a , b , c;
}");
		}

		[Test()]
		public void TestFixedFieldSpacesBeforeComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			policy.SpaceAfterFieldDeclarationComma = true;
			policy.SpaceBeforeFieldDeclarationComma = true;
			
			Test (policy, @"class Test {
	fixed int a[10]           ,                   b[10],          c[10];
}",
	@"class Test {
	fixed int a[10] , b[10] , c[10];
}");
		}

		[Test()]
		public void TestConstFieldSpacesBeforeComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			policy.SpaceAfterFieldDeclarationComma = false;
			policy.SpaceBeforeFieldDeclarationComma = false;
			
			Test (policy, @"class Test {
	const int a = 1           ,                   b = 2,          c = 3;
}",
@"class Test {
	const int a = 1,b = 2,c = 3;
}");
		}

		[Test()]
		public void TestBeforeMethodDeclarationParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeMethodDeclarationParentheses = true;
			
			Test (policy, @"public abstract class Test
{
	public abstract Test TestMethod();
}",
@"public abstract class Test
{
	public abstract Test TestMethod ();
}");
		}

		[Test()]
		public void TestBeforeConstructorDeclarationParenthesesDestructorCase ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeConstructorDeclarationParentheses = true;
			
			Test (policy, @"class Test
{
	~Test()
	{
	}
}",
@"class Test
{
	~Test ()
	{
	}
}");
		}

		static void TestBinaryOperator (CSharpFormattingOptions policy, string op)
		{
			var result = GetResult (policy, "class Test { void TestMe () { result = left" + op + "right; } }");
			
			int i1 = result.Text.IndexOf ("left");
			int i2 = result.Text.IndexOf ("right") + "right".Length;
			if (i1 < 0 || i2 < 0)
				Assert.Fail ("text invalid:" + result.Text);
			Assert.AreEqual ("left " + op + " right", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestSpacesAroundMultiplicativeOperator ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAroundMultiplicativeOperator = true;
			
			TestBinaryOperator (policy, "*");
			TestBinaryOperator (policy, "/");
		}

		[Test()]
		public void TestSpacesAroundShiftOperator ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAroundShiftOperator = true;
			TestBinaryOperator (policy, "<<");
			TestBinaryOperator (policy, ">>");
		}

		[Test()]
		public void TestSpacesAroundAdditiveOperator ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAroundAdditiveOperator = true;
			
			TestBinaryOperator (policy, "+");
			TestBinaryOperator (policy, "-");
		}

		[Test()]
		public void TestSpacesAroundBitwiseOperator ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAroundBitwiseOperator = true;
			
			TestBinaryOperator (policy, "&");
			TestBinaryOperator (policy, "|");
			TestBinaryOperator (policy, "^");
		}

		[Test()]
		public void TestSpacesAroundRelationalOperator ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAroundRelationalOperator = true;
			
			TestBinaryOperator (policy, "<");
			TestBinaryOperator (policy, "<=");
			TestBinaryOperator (policy, ">");
			TestBinaryOperator (policy, ">=");
		}

		[Test()]
		public void TestSpacesAroundEqualityOperator ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAroundEqualityOperator = true;
			
			TestBinaryOperator (policy, "==");
			TestBinaryOperator (policy, "!=");
		}

		[Test()]
		public void TestSpacesAroundLogicalOperator ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAroundLogicalOperator = true;
			
			TestBinaryOperator (policy, "&&");
			TestBinaryOperator (policy, "||");
		}

		[Test()]
		public void TestConditionalOperator ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeConditionalOperatorCondition = true;
			policy.SpaceAfterConditionalOperatorCondition = true;
			policy.SpaceBeforeConditionalOperatorSeparator = true;
			policy.SpaceAfterConditionalOperatorSeparator = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		result = condition?trueexpr:falseexpr;
	}
}");
			int i1 = result.Text.IndexOf ("condition");
			int i2 = result.Text.IndexOf ("falseexpr") + "falseexpr".Length;
			Assert.AreEqual (@"condition ? trueexpr : falseexpr", result.GetText (i1, i2 - i1));
			
			
			policy.SpaceBeforeConditionalOperatorCondition = false;
			policy.SpaceAfterConditionalOperatorCondition = false;
			policy.SpaceBeforeConditionalOperatorSeparator = false;
			policy.SpaceAfterConditionalOperatorSeparator = false;
			
			result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		result = true ? trueexpr : falseexpr;
	}
}");
			i1 = result.Text.IndexOf ("true");
			i2 = result.Text.IndexOf ("falseexpr") + "falseexpr".Length;
			Assert.AreEqual (@"true?trueexpr:falseexpr", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeMethodCallParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeMethodCallParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		MethodCall();
	}
}");
			
			int i1 = result.Text.IndexOf ("MethodCall");
			int i2 = result.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"MethodCall ();", result.GetText (i1, i2 - i1));
			
			
			result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		MethodCall                         				();
	}
}");
			policy.SpaceBeforeMethodCallParentheses = false;
			
			result = GetResult (policy, result.Text);
			i1 = result.Text.IndexOf ("MethodCall");
			i2 = result.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"MethodCall();", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinMethodCallParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceWithinMethodCallParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		MethodCall(true);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( true )", result.GetText (i1, i2 - i1));
			
			
			policy.SpaceWithinMethodCallParentheses = false;
			result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		MethodCall( true );
	}
}");
			
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(true)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeIfParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeIfParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		if(true);
	}
}");
			int i1 = result.Text.IndexOf ("if");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"if (true)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinIfParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinIfParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		if (true);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( true )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeWhileParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeWhileParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		while(true);
	}
}");
			int i1 = result.Text.IndexOf ("while");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"while (true)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinWhileParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinWhileParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		while (true);
	}
}");
			
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( true )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeForParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeForParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		for(;;);
	}
}");
			int i1 = result.Text.IndexOf ("for");
			int i2 = result.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"for (", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinForParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinForParentheses = true;
		
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		for(;;);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( ;; )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeForeachParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeForeachParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		foreach(var o in list);
	}
}");
			int i1 = result.Text.IndexOf ("foreach");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"foreach (var o in list)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinForeachParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinForeachParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		foreach(var o in list);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( var o in list )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeCatchParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeCatchParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
  try {} catch(Exception) {}
	}
}");
			int i1 = result.Text.IndexOf ("catch");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"catch (Exception)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinCatchParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinCatchParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		try {} catch(Exception) {}
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( Exception )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeLockParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeLockParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		lock(this) {}
	}
}");
			int i1 = result.Text.IndexOf ("lock");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"lock (this)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinLockParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinLockParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		lock(this) {}
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( this )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestSpacesAfterForSemicolon ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAfterForSemicolon = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		for (int i;true;i++) ;
	}
}");
			int i1 = result.Text.LastIndexOf ("for");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			
			Assert.AreEqual (@"for (int i; true; i++)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestSpacesBeforeForSemicolon ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeForSemicolon = true;
			policy.SpaceAfterForSemicolon = false;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		for (int i;true;i++) ;
	}
}");
			int i1 = result.Text.LastIndexOf ("for");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			
			Assert.AreEqual (@"for (int i ;true ;i++)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestSpacesAfterTypecast ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAfterTypecast = true;
			
			var result = GetResult (policy, @"class Test {
	Test TestMe ()
	{
return (Test)null;
	}
}");
			int i1 = result.Text.LastIndexOf ("return");
			int i2 = result.Text.LastIndexOf ("null") + "null".Length;
			
			Assert.AreEqual (@"return (Test) null", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeUsingParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeUsingParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		using(a) {}
	}
}");
			int i1 = result.Text.IndexOf ("using");
			int i2 = result.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"using (", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinUsingParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinUsingParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		using(a) {}
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( a )", result.GetText (i1, i2 - i1));
		}

		static void TestAssignmentOperator (CSharpFormattingOptions policy, string op)
		{
			var result = GetResult (policy, "class Test { void TestMe () { left" + op + "right; } }");
			
			int i1 = result.Text.IndexOf ("left");
			int i2 = result.Text.IndexOf ("right") + "right".Length;
			if (i1 < 0 || i2 < 0)
				Assert.Fail ("text invalid:" + result.Text);
			Assert.AreEqual ("left " + op + " right", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestAroundAssignmentSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAroundAssignment = true;
			
			TestAssignmentOperator (policy, "=");
			TestAssignmentOperator (policy, "*=");
			TestAssignmentOperator (policy, "/=");
			TestAssignmentOperator (policy, "+=");
			TestAssignmentOperator (policy, "%=");
			TestAssignmentOperator (policy, "-=");
			TestAssignmentOperator (policy, "<<=");
			TestAssignmentOperator (policy, ">>=");
			TestAssignmentOperator (policy, "&=");
			TestAssignmentOperator (policy, "|=");
			TestAssignmentOperator (policy, "^=");
		}

		[Test()]
		public void TestAroundAssignmentSpaceInDeclarations ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAroundAssignment = true;
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		int left=right;
	}
}");
			
			int i1 = result.Text.LastIndexOf ("left");
			int i2 = result.Text.LastIndexOf ("right") + "right".Length;
			Assert.AreEqual (@"left = right", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeSwitchParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeSwitchParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		switch (test) { default: break; }
	}
}");
			int i1 = result.Text.IndexOf ("switch");
			int i2 = result.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"switch (", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinSwitchParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinSwitchParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		switch (test) { default: break; }
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( test )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		c = (test);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( test )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinMethodDeclarationParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceWithinMethodDeclarationParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe (int a)
	{
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int a )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinCastParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinCastParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = (int)b;
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinSizeOfParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinSizeOfParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = sizeof(int);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeSizeOfParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeSizeOfParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = sizeof(int);
	}
}");
			int i1 = result.Text.LastIndexOf ("sizeof");
			int i2 = result.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"sizeof (", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinTypeOfParenthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinTypeOfParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = typeof(int);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeTypeOfParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeTypeOfParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = typeof(int);
	}
}");
			
			int i1 = result.Text.LastIndexOf ("typeof");
			int i2 = result.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"typeof (", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinCheckedExpressionParanthesesSpace ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinCheckedExpressionParantheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = checked(a + b);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( a + b )", result.GetText (i1, i2 - i1));
			
			result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = unchecked(a + b);
	}
}");
			
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( a + b )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestSpaceBeforeNewParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeNewParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		new Test();
	}
}");
			int i1 = result.Text.LastIndexOf ("new");
			int i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test ();", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinNewParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinNewParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		new Test (1);
	}
}");
			int i1 = result.Text.LastIndexOf ("new");
			int i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test ( 1 );", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBetweenEmptyNewParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesBetweenEmptyNewParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		new Test ();
	}
}");
			int i1 = result.Text.LastIndexOf ("new");
			int i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test ( );", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeNewParameterComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeNewParameterComma = true;
			policy.SpaceAfterNewParameterComma = false;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		new Test (1,2);
	}
}");
			int i1 = result.Text.LastIndexOf ("new");
			int i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test (1 ,2);", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestAfterNewParameterComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAfterNewParameterComma = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		new Test (1,2);
	}
}");
			int i1 = result.Text.LastIndexOf ("new");
			int i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test (1, 2);", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestFieldDeclarationComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeFieldDeclarationComma = false;
			policy.SpaceAfterFieldDeclarationComma = true;
			
			var result = GetResult (policy, @"class Test {
	int a,b,c;
}");
			int i1 = result.Text.LastIndexOf ("int");
			int i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a, b, c;", result.GetText (i1, i2 - i1));
			policy.SpaceBeforeFieldDeclarationComma = true;
			
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("int");
			i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a , b , c;", result.GetText (i1, i2 - i1));
			
			policy.SpaceBeforeFieldDeclarationComma = false;
			policy.SpaceAfterFieldDeclarationComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("int");
			i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a,b,c;", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeMethodDeclarationParameterComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeMethodDeclarationParameterComma = true;
			policy.SpaceAfterMethodDeclarationParameterComma = false;
			
			var result = GetResult (policy, @"class Test {
	public void Foo (int a,int b,int c) {}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a ,int b ,int c)", result.GetText (i1, i2 - i1));
			
			policy.SpaceBeforeMethodDeclarationParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestAfterMethodDeclarationParameterComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeMethodDeclarationParameterComma = false;
			policy.SpaceAfterMethodDeclarationParameterComma = true;
			
			var result = GetResult (policy, @"class Test {
	public void Foo (int a,int b,int c) {}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a, int b, int c)", result.GetText (i1, i2 - i1));
			
			policy.SpaceAfterMethodDeclarationParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestSpacesInLambdaExpression ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinWhileParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		var v = x=>x!=null;
	}
}");
			int i1 = result.Text.IndexOf ("x");
			int i2 = result.Text.LastIndexOf ("null") + "null".Length;
			Assert.AreEqual (@"x => x != null", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeLocalVariableDeclarationComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeLocalVariableDeclarationComma = true;
			policy.SpaceAfterLocalVariableDeclarationComma = false;

			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		int a,b,c;
	}
}");
			int i1 = result.Text.IndexOf ("int");
			int i2 = result.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a ,b ,c;", result.GetText (i1, i2 - i1));

			result = GetResult (policy, result.Text);

			policy.SpaceBeforeLocalVariableDeclarationComma = false;

			result = GetResult (policy, result.Text);
			i1 = result.Text.IndexOf ("int");
			i2 = result.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a,b,c;", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestLocalVariableDeclarationComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeLocalVariableDeclarationComma = true;
			policy.SpaceAfterLocalVariableDeclarationComma = true;

			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		int a = 5,b = 6,c;
	}
}");
			int i1 = result.Text.IndexOf ("int");
			int i2 = result.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a = 5 , b = 6 , c;", result.GetText (i1, i2 - i1));

			result = GetResult (policy, result.Text);

			policy.SpaceBeforeLocalVariableDeclarationComma = false;
			policy.SpaceAfterLocalVariableDeclarationComma = false;

			result = GetResult (policy, result.Text);
			i1 = result.Text.IndexOf ("int");
			i2 = result.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a = 5,b = 6,c;", result.GetText (i1, i2 - i1));
		}
		
		[Test()]
		public void TestLocalVariableWithGenerics ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeLocalVariableDeclarationComma = true;
			policy.SpaceAfterLocalVariableDeclarationComma = true;

			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		List<Test> a;
	}
}");
			int i1 = result.Text.IndexOf ("List");
			int i2 = result.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"List<Test> a;", result.GetText (i1, i2 - i1));

		}

		#region Constructors
		
		[Test()]
		public void TestBeforeConstructorDeclarationParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeConstructorDeclarationParentheses = true;
			
			var result = GetResult (policy, @"class Test
{
	Test()
	{
	}
}");
			
			Assert.AreEqual (NormalizeNewlines(@"class Test
{
	Test ()
	{
	}
}"), result.Text);
		}

		[Test()]
		public void TestBeforeConstructorDeclarationParameterComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeConstructorDeclarationParameterComma = true;
			policy.SpaceAfterConstructorDeclarationParameterComma = false;
			
			var result = GetResult (policy, @"class Test {
	public Test (int a,int b,int c) {}
}");
			
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a ,int b ,int c)", result.GetText (i1, i2 - i1));
			
			policy.SpaceBeforeConstructorDeclarationParameterComma = false;
			result = GetResult (policy, result.Text);
			
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestAfterConstructorDeclarationParameterComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeConstructorDeclarationParameterComma = false;
			policy.SpaceAfterConstructorDeclarationParameterComma = true;
			
			var result = GetResult (policy, @"class Test {
	public Test (int a,int b,int c) {}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a, int b, int c)", result.GetText (i1, i2 - i1));
			
			policy.SpaceAfterConstructorDeclarationParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinConstructorDeclarationParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceWithinConstructorDeclarationParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	Test (int a)
	{
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int a )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBetweenEmptyConstructorDeclarationParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBetweenEmptyConstructorDeclarationParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	Test ()
	{
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( )", result.GetText (i1, i2 - i1));
		}

		#endregion
		
		#region Delegates
		[Test()]
		public void TestBeforeDelegateDeclarationParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeDelegateDeclarationParentheses = true;
			
			var result = GetResult (policy, @"delegate void Test();");
			
			Assert.AreEqual (@"delegate void Test ();", result.Text);
		}

		[Test()]
		public void TestBeforeDelegateDeclarationParenthesesComplex ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeDelegateDeclarationParentheses = true;
			
			var result = GetResult (policy, "delegate void TestDelegate\t\t\t();");
			
			Assert.AreEqual (@"delegate void TestDelegate ();", result.Text);
		}

		[Test()]
		public void TestBeforeDelegateDeclarationParameterComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeDelegateDeclarationParameterComma = true;
			policy.SpaceAfterDelegateDeclarationParameterComma = false;
			
			var result = GetResult (policy, @"delegate void Test (int a,int b,int c);");
			
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a ,int b ,int c)", result.GetText (i1, i2 - i1));
			
			policy.SpaceBeforeDelegateDeclarationParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestAfterDelegateDeclarationParameterComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeDelegateDeclarationParameterComma = false;
			policy.SpaceAfterDelegateDeclarationParameterComma = true;
			
			var result = GetResult (policy, @"delegate void Test (int a,int b,int c);");
			
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a, int b, int c)", result.GetText (i1, i2 - i1));
			
			policy.SpaceAfterDelegateDeclarationParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinDelegateDeclarationParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceWithinDelegateDeclarationParentheses = true;
			var result = GetResult (policy, @"delegate void Test (int a);");
			
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int a )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBetweenEmptyDelegateDeclarationParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBetweenEmptyDelegateDeclarationParentheses = true;
			var result = GetResult (policy, @"delegate void Test();");
			
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( )", result.GetText (i1, i2 - i1));
		}

		#endregion
		
		#region Method invocations
		[Test()]
		public void TestBeforeMethodCallParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeMethodCallParentheses = true;
			
			var result = GetResult (policy, @"class FooBar
{
	public void Foo ()
	{
		Test();
	}
}");
			
			Assert.AreEqual (NormalizeNewlines(@"class FooBar
{
	public void Foo ()
	{
		Test ();
	}
}"), result.Text);
		}

		[Test()]
		public void TestBeforeMethodCallParameterComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeMethodCallParameterComma = true;
			policy.SpaceAfterMethodCallParameterComma = false;
			
			var result = GetResult (policy, @"class FooBar
{
	public void Foo ()
	{
		Test(a,b,c);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a ,b ,c)", result.GetText (i1, i2 - i1));
			
			policy.SpaceBeforeMethodCallParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a,b,c)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestAfterMethodCallParameterComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeMethodCallParameterComma = false;
			policy.SpaceAfterMethodCallParameterComma = true;
			
			var result = GetResult (policy, @"class FooBar
{
	public void Foo ()
	{
		Test(a,b,c);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a, b, c)", result.GetText (i1, i2 - i1));
			
			policy.SpaceAfterMethodCallParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a,b,c)", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinMethodCallParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceWithinMethodCallParentheses = true;
			
			var result = GetResult (policy, @"class FooBar
{
	public void Foo ()
	{
		Test(a);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( a )", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestBetweenEmptyMethodCallParentheses ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBetweenEmptyMethodCallParentheses = true;
			
			var result = GetResult (policy, @"class FooBar
{
	public void Foo ()
	{
		Test();
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( )", result.GetText (i1, i2 - i1));
		}

		#endregion
		
		#region Indexer declarations
		[Test()]
		public void TestBeforeIndexerDeclarationBracket ()
		{
			
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeIndexerDeclarationBracket = true;
			
			var result = GetResult (policy, @"class FooBar
{
	public int this[int a, int b] {
		get {
			return a + b;	
		}
	}
}");
			Assert.AreEqual (NormalizeNewlines(@"class FooBar
{
	public int this [int a, int b] {
		get {
			return a + b;	
		}
	}
}"), result.Text);
		}

		[Test()]
		public void TestBeforeIndexerDeclarationParameterComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeIndexerDeclarationParameterComma = true;
			policy.SpaceAfterIndexerDeclarationParameterComma = false;
			
			var result = GetResult (policy, @"class FooBar
{
	public int this[int a,int b] {
		get {
			return a + b;	
		}
	}
}");
			int i1 = result.Text.LastIndexOf ("[");
			int i2 = result.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[int a ,int b]", result.GetText (i1, i2 - i1));

		}

		[Test()]
		public void TestAfterIndexerDeclarationParameterComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAfterIndexerDeclarationParameterComma = true;
			
			var result = GetResult (policy, @"class FooBar
{
	public int this[int a,int b] {
		get {
			return a + b;	
		}
	}
}");
			int i1 = result.Text.LastIndexOf ("[");
			int i2 = result.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[int a, int b]", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinIndexerDeclarationBracket ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceWithinIndexerDeclarationBracket = true;
			
			var result = GetResult (policy, @"class FooBar
{
	public int this[int a, int b] {
		get {
			return a + b;	
		}
	}
}");
			int i1 = result.Text.LastIndexOf ("[");
			int i2 = result.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[ int a, int b ]", result.GetText (i1, i2 - i1));
		}

		#endregion

		#region Brackets
		
		[Test()]
		public void TestSpacesWithinBrackets ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesWithinBrackets = true;
			policy.SpacesBeforeBrackets = false;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		this[0] = 5;
	}
}");
			Assert.AreEqual (NormalizeNewlines(@"class Test
{
	void TestMe ()
	{
		this[ 0 ] = 5;
	}
}"), result.Text);
			
			
		}

		[Test()]
		public void TestSpacesBeforeBrackets ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpacesBeforeBrackets = true;
			
			var result = GetResult (policy, @"class Test
{
	void TestMe ()
	{
		this[0] = 5;
	}
}");
			Assert.AreEqual (NormalizeNewlines(@"class Test
{
	void TestMe ()
	{
		this [0] = 5;
	}
}"), result.Text);
			
			
		}

		[Test()]
		public void TestBeforeBracketComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeBracketComma = true;
			policy.SpaceAfterBracketComma = false;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		this[1,2,3] = 5;
	}
}");
			
			int i1 = result.Text.LastIndexOf ("[");
			int i2 = result.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[1 ,2 ,3]", result.GetText (i1, i2 - i1));
		}

		[Test()]
		public void TestAfterBracketComma ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceAfterBracketComma = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		this[1,2,3] = 5;
	}
}");
			
			int i1 = result.Text.LastIndexOf ("[");
			int i2 = result.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[1, 2, 3]", result.GetText (i1, i2 - i1));
		}

		#endregion
		
		[Test()]
		public void TestSpacesBeforeArrayDeclarationBrackets ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceBeforeArrayDeclarationBrackets = true;
			
			var result = GetResult (policy, @"class Test {
	int[] a;
	int[][] b;
}");
			
			Assert.AreEqual (NormalizeNewlines(@"class Test
{
	int [] a;
	int [][] b;
}"), result.Text);
			
			
		}

		[Test()]
		public void TestRemoveWhitespacesBeforeSemicolon()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			var result = GetResult(policy, @"class Test {
	void TestMe ()
	{
		Foo ()        ;
	}
}");
			int i1 = result.Text.IndexOf("Foo");
			int i2 = result.Text.LastIndexOf(";") + ";".Length;
			Assert.AreEqual(@"Foo ();", result.GetText(i1, i2 - i1));
		}

		[Test()]
		public void TestSpaceInNamedArgumentAfterDoubleColon()
		{
			var policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceInNamedArgumentAfterDoubleColon = true;
			var result = GetResult(policy, @"class Test {
	void TestMe ()
	{
		Foo (bar:expr);
	}
}");
			int i1 = result.Text.IndexOf("Foo");
			int i2 = result.Text.LastIndexOf(";") + ";".Length;
			Assert.AreEqual(@"Foo (bar: expr);", result.GetText(i1, i2 - i1));
		}

		[Test()]
		public void TestSpaceInNamedArgumentAfterDoubleColon2()
		{
			var policy = FormattingOptionsFactory.CreateMono ();
			policy.SpaceInNamedArgumentAfterDoubleColon = false;
			var result = GetResult(policy, @"class Test {
	void TestMe ()
	{
		Foo (bar:                      expr);
	}
}");
			int i1 = result.Text.IndexOf("Foo");
			int i2 = result.Text.LastIndexOf(";") + ";".Length;
			Assert.AreEqual(@"Foo (bar:expr);", result.GetText(i1, i2 - i1));
		}





		
	}
}
