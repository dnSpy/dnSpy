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

namespace ICSharpCode.NRefactory.FormattingTests
{
	[TestFixture()]
	public class TestSpacingVisitor : TestBase
	{
		[Test()]
		public void TestFieldSpacesBeforeComma1 ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			policy.SpaceBeforeFieldDeclarationComma = false;
			policy.SpaceAfterFieldDeclarationComma = false;
			
			Test (policy, @"class Test {
	int a           ,                   b,          c;
}",
@"class Test {
	int a,b,c;
}");
		}

		[Test()]
		public void TestFieldSpacesBeforeComma2 ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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

		static void TestBinaryOperator (CSharpFormattingPolicy policy, string op)
		{
			var result = GetResult (policy, "class Test { void TestMe () { result = left" + op + "right; } }");
			
			int i1 = result.Text.IndexOf ("left");
			int i2 = result.Text.IndexOf ("right") + "right".Length;
			if (i1 < 0 || i2 < 0)
				Assert.Fail ("text invalid:" + result.Text);
			Assert.AreEqual ("left " + op + " right", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestSpacesAroundMultiplicativeOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundMultiplicativeOperator = true;
			
			TestBinaryOperator (policy, "*");
			TestBinaryOperator (policy, "/");
		}

		[Test()]
		public void TestSpacesAroundShiftOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundShiftOperator = true;
			TestBinaryOperator (policy, "<<");
			TestBinaryOperator (policy, ">>");
		}

		[Test()]
		public void TestSpacesAroundAdditiveOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundAdditiveOperator = true;
			
			TestBinaryOperator (policy, "+");
			TestBinaryOperator (policy, "-");
		}

		[Test()]
		public void TestSpacesAroundBitwiseOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundBitwiseOperator = true;
			
			TestBinaryOperator (policy, "&");
			TestBinaryOperator (policy, "|");
			TestBinaryOperator (policy, "^");
		}

		[Test()]
		public void TestSpacesAroundRelationalOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundRelationalOperator = true;
			
			TestBinaryOperator (policy, "<");
			TestBinaryOperator (policy, "<=");
			TestBinaryOperator (policy, ">");
			TestBinaryOperator (policy, ">=");
		}

		[Test()]
		public void TestSpacesAroundEqualityOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundEqualityOperator = true;
			
			TestBinaryOperator (policy, "==");
			TestBinaryOperator (policy, "!=");
		}

		[Test()]
		public void TestSpacesAroundLogicalOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundLogicalOperator = true;
			
			TestBinaryOperator (policy, "&&");
			TestBinaryOperator (policy, "||");
		}

		[Test()]
		public void TestConditionalOperator ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			Assert.AreEqual (@"condition ? trueexpr : falseexpr", result.GetTextAt (i1, i2 - i1));
			
			
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
			Assert.AreEqual (@"true?trueexpr:falseexpr", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeMethodCallParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeMethodCallParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		MethodCall();
	}
}");
			
			int i1 = result.Text.IndexOf ("MethodCall");
			int i2 = result.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"MethodCall ();", result.GetTextAt (i1, i2 - i1));
			
			
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
			Assert.AreEqual (@"MethodCall();", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinMethodCallParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceWithinMethodCallParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		MethodCall(true);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( true )", result.GetTextAt (i1, i2 - i1));
			
			
			policy.SpaceWithinMethodCallParentheses = false;
			result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		MethodCall( true );
	}
}");
			
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(true)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeIfParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeIfParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		if(true);
	}
}");
			int i1 = result.Text.IndexOf ("if");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"if (true)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinIfParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinIfParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		if (true);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( true )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeWhileParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeWhileParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		while(true);
	}
}");
			int i1 = result.Text.IndexOf ("while");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"while (true)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinWhileParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinWhileParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		while (true);
	}
}");
			
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( true )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeForParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeForParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		for(;;);
	}
}");
			int i1 = result.Text.IndexOf ("for");
			int i2 = result.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"for (", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinForParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinForParentheses = true;
		
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		for(;;);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( ;; )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeForeachParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeForeachParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		foreach(var o in list);
	}
}");
			int i1 = result.Text.IndexOf ("foreach");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"foreach (var o in list)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinForeachParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinForeachParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		foreach(var o in list);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( var o in list )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeCatchParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeCatchParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
  try {} catch(Exception) {}
	}
}");
			int i1 = result.Text.IndexOf ("catch");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"catch (Exception)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinCatchParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinCatchParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		try {} catch(Exception) {}
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( Exception )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeLockParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeLockParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		lock(this) {}
	}
}");
			int i1 = result.Text.IndexOf ("lock");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"lock (this)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinLockParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinLockParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		lock(this) {}
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( this )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestSpacesAfterForSemicolon ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAfterForSemicolon = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		for (int i;true;i++) ;
	}
}");
			int i1 = result.Text.LastIndexOf ("for");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			
			Assert.AreEqual (@"for (int i; true; i++)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestSpacesBeforeForSemicolon ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			
			Assert.AreEqual (@"for (int i ;true ;i++)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestSpacesAfterTypecast ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAfterTypecast = true;
			
			var result = GetResult (policy, @"class Test {
	Test TestMe ()
	{
return (Test)null;
	}
}");
			int i1 = result.Text.LastIndexOf ("return");
			int i2 = result.Text.LastIndexOf ("null") + "null".Length;
			
			Assert.AreEqual (@"return (Test) null", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeUsingParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeUsingParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		using(a) {}
	}
}");
			int i1 = result.Text.IndexOf ("using");
			int i2 = result.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"using (", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinUsingParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinUsingParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		using(a) {}
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( a )", result.GetTextAt (i1, i2 - i1));
		}

		static void TestAssignmentOperator (CSharpFormattingPolicy policy, string op)
		{
			var result = GetResult (policy, "class Test { void TestMe () { left" + op + "right; } }");
			
			int i1 = result.Text.IndexOf ("left");
			int i2 = result.Text.IndexOf ("right") + "right".Length;
			if (i1 < 0 || i2 < 0)
				Assert.Fail ("text invalid:" + result.Text);
			Assert.AreEqual ("left " + op + " right", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestAroundAssignmentSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAroundAssignment = true;
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		int left=right;
	}
}");
			
			int i1 = result.Text.LastIndexOf ("left");
			int i2 = result.Text.LastIndexOf ("right") + "right".Length;
			Assert.AreEqual (@"left = right", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeSwitchParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeSwitchParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		switch (test) { default: break; }
	}
}");
			int i1 = result.Text.IndexOf ("switch");
			int i2 = result.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"switch (", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinSwitchParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinSwitchParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		switch (test) { default: break; }
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( test )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		c = (test);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( test )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinMethodDeclarationParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceWithinMethodDeclarationParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe (int a)
	{
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int a )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinCastParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinCastParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = (int)b;
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinSizeOfParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinSizeOfParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = sizeof(int);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeSizeOfParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeSizeOfParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = sizeof(int);
	}
}");
			int i1 = result.Text.LastIndexOf ("sizeof");
			int i2 = result.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"sizeof (", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinTypeOfParenthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinTypeOfParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = typeof(int);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeTypeOfParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeTypeOfParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = typeof(int);
	}
}");
			
			int i1 = result.Text.LastIndexOf ("typeof");
			int i2 = result.Text.LastIndexOf ("(") + "(".Length;
			Assert.AreEqual (@"typeof (", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinCheckedExpressionParanthesesSpace ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinCheckedExpressionParantheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = checked(a + b);
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( a + b )", result.GetTextAt (i1, i2 - i1));
			
			result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		a = unchecked(a + b);
	}
}");
			
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( a + b )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestSpaceBeforeNewParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeNewParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		new Test();
	}
}");
			int i1 = result.Text.LastIndexOf ("new");
			int i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test ();", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinNewParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinNewParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		new Test (1);
	}
}");
			int i1 = result.Text.LastIndexOf ("new");
			int i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test ( 1 );", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBetweenEmptyNewParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesBetweenEmptyNewParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		new Test ();
	}
}");
			int i1 = result.Text.LastIndexOf ("new");
			int i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test ( );", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeNewParameterComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			Assert.AreEqual (@"new Test (1 ,2);", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestAfterNewParameterComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAfterNewParameterComma = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		new Test (1,2);
	}
}");
			int i1 = result.Text.LastIndexOf ("new");
			int i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"new Test (1, 2);", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestFieldDeclarationComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeFieldDeclarationComma = false;
			policy.SpaceAfterFieldDeclarationComma = true;
			
			var result = GetResult (policy, @"class Test {
	int a,b,c;
}");
			int i1 = result.Text.LastIndexOf ("int");
			int i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a, b, c;", result.GetTextAt (i1, i2 - i1));
			policy.SpaceBeforeFieldDeclarationComma = true;
			
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("int");
			i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a , b , c;", result.GetTextAt (i1, i2 - i1));
			
			policy.SpaceBeforeFieldDeclarationComma = false;
			policy.SpaceAfterFieldDeclarationComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("int");
			i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a,b,c;", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeMethodDeclarationParameterComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeMethodDeclarationParameterComma = true;
			policy.SpaceAfterMethodDeclarationParameterComma = false;
			
			var result = GetResult (policy, @"class Test {
	public void Foo (int a,int b,int c) {}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a ,int b ,int c)", result.GetTextAt (i1, i2 - i1));
			
			policy.SpaceBeforeMethodDeclarationParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestAfterMethodDeclarationParameterComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeMethodDeclarationParameterComma = false;
			policy.SpaceAfterMethodDeclarationParameterComma = true;
			
			var result = GetResult (policy, @"class Test {
	public void Foo (int a,int b,int c) {}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a, int b, int c)", result.GetTextAt (i1, i2 - i1));
			
			policy.SpaceAfterMethodDeclarationParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestSpacesInLambdaExpression ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinWhileParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		var v = x=>x!=null;
	}
}");
			int i1 = result.Text.IndexOf ("x");
			int i2 = result.Text.LastIndexOf ("null") + "null".Length;
			Assert.AreEqual (@"x => x != null", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBeforeLocalVariableDeclarationComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			Assert.AreEqual (@"int a ,b ,c;", result.GetTextAt (i1, i2 - i1));

			result = GetResult (policy, result.Text);

			policy.SpaceBeforeLocalVariableDeclarationComma = false;

			result = GetResult (policy, result.Text);
			i1 = result.Text.IndexOf ("int");
			i2 = result.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a,b,c;", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestLocalVariableDeclarationComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			Assert.AreEqual (@"int a = 5 , b = 6 , c;", result.GetTextAt (i1, i2 - i1));

			result = GetResult (policy, result.Text);

			policy.SpaceBeforeLocalVariableDeclarationComma = false;
			policy.SpaceAfterLocalVariableDeclarationComma = false;

			result = GetResult (policy, result.Text);
			i1 = result.Text.IndexOf ("int");
			i2 = result.Text.IndexOf (";") + ";".Length;
			Assert.AreEqual (@"int a = 5,b = 6,c;", result.GetTextAt (i1, i2 - i1));
		}

		#region Constructors
		
		[Test()]
		public void TestBeforeConstructorDeclarationParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeConstructorDeclarationParentheses = true;
			
			var result = GetResult (policy, @"class Test
{
	Test()
	{
	}
}");
			
			Assert.AreEqual (@"class Test
{
	Test ()
	{
	}
}", result.Text);
		}

		[Test()]
		public void TestBeforeConstructorDeclarationParameterComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeConstructorDeclarationParameterComma = true;
			policy.SpaceAfterConstructorDeclarationParameterComma = false;
			
			var result = GetResult (policy, @"class Test {
	public Test (int a,int b,int c) {}
}");
			
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a ,int b ,int c)", result.GetTextAt (i1, i2 - i1));
			
			policy.SpaceBeforeConstructorDeclarationParameterComma = false;
			result = GetResult (policy, result.Text);
			
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestAfterConstructorDeclarationParameterComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeConstructorDeclarationParameterComma = false;
			policy.SpaceAfterConstructorDeclarationParameterComma = true;
			
			var result = GetResult (policy, @"class Test {
	public Test (int a,int b,int c) {}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a, int b, int c)", result.GetTextAt (i1, i2 - i1));
			
			policy.SpaceAfterConstructorDeclarationParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinConstructorDeclarationParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceWithinConstructorDeclarationParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	Test (int a)
	{
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int a )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBetweenEmptyConstructorDeclarationParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBetweenEmptyConstructorDeclarationParentheses = true;
			
			var result = GetResult (policy, @"class Test {
	Test ()
	{
	}
}");
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( )", result.GetTextAt (i1, i2 - i1));
		}

		#endregion
		
		#region Delegates
		[Test()]
		public void TestBeforeDelegateDeclarationParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeDelegateDeclarationParentheses = true;
			
			var result = GetResult (policy, @"delegate void Test();");
			
			Assert.AreEqual (@"delegate void Test ();", result.Text);
		}

		[Test()]
		public void TestBeforeDelegateDeclarationParenthesesComplex ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeDelegateDeclarationParentheses = true;
			
			var result = GetResult (policy, "delegate void TestDelegate\t\t\t();");
			
			Assert.AreEqual (@"delegate void TestDelegate ();", result.Text);
		}

		[Test()]
		public void TestBeforeDelegateDeclarationParameterComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeDelegateDeclarationParameterComma = true;
			policy.SpaceAfterDelegateDeclarationParameterComma = false;
			
			var result = GetResult (policy, @"delegate void Test (int a,int b,int c);");
			
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a ,int b ,int c)", result.GetTextAt (i1, i2 - i1));
			
			policy.SpaceBeforeDelegateDeclarationParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestAfterDelegateDeclarationParameterComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeDelegateDeclarationParameterComma = false;
			policy.SpaceAfterDelegateDeclarationParameterComma = true;
			
			var result = GetResult (policy, @"delegate void Test (int a,int b,int c);");
			
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a, int b, int c)", result.GetTextAt (i1, i2 - i1));
			
			policy.SpaceAfterDelegateDeclarationParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(int a,int b,int c)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinDelegateDeclarationParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceWithinDelegateDeclarationParentheses = true;
			var result = GetResult (policy, @"delegate void Test (int a);");
			
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( int a )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBetweenEmptyDelegateDeclarationParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBetweenEmptyDelegateDeclarationParentheses = true;
			var result = GetResult (policy, @"delegate void Test();");
			
			int i1 = result.Text.LastIndexOf ("(");
			int i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"( )", result.GetTextAt (i1, i2 - i1));
		}

		#endregion
		
		#region Method invocations
		[Test()]
		public void TestBeforeMethodCallParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeMethodCallParentheses = true;
			
			var result = GetResult (policy, @"class FooBar
{
	public void Foo ()
	{
		Test();
	}
}");
			
			Assert.AreEqual (@"class FooBar
{
	public void Foo ()
	{
		Test ();
	}
}", result.Text);
		}

		[Test()]
		public void TestBeforeMethodCallParameterComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			Assert.AreEqual (@"(a ,b ,c)", result.GetTextAt (i1, i2 - i1));
			
			policy.SpaceBeforeMethodCallParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a,b,c)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestAfterMethodCallParameterComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			Assert.AreEqual (@"(a, b, c)", result.GetTextAt (i1, i2 - i1));
			
			policy.SpaceAfterMethodCallParameterComma = false;
			result = GetResult (policy, result.Text);
			i1 = result.Text.LastIndexOf ("(");
			i2 = result.Text.LastIndexOf (")") + ")".Length;
			Assert.AreEqual (@"(a,b,c)", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinMethodCallParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			Assert.AreEqual (@"( a )", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestBetweenEmptyMethodCallParentheses ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			Assert.AreEqual (@"( )", result.GetTextAt (i1, i2 - i1));
		}

		#endregion
		
		#region Indexer declarations
		[Test()]
		public void TestBeforeIndexerDeclarationBracket ()
		{
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeIndexerDeclarationBracket = true;
			
			var result = GetResult (policy, @"class FooBar
{
	public int this[int a, int b] {
		get {
			return a + b;	
		}
	}
}");
			Assert.AreEqual (@"class FooBar
{
	public int this [int a, int b] {
		get {
			return a + b;	
		}
	}
}", result.Text);
		}

		[Test()]
		public void TestBeforeIndexerDeclarationParameterComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			Assert.AreEqual (@"[int a ,int b]", result.GetTextAt (i1, i2 - i1));

		}

		[Test()]
		public void TestAfterIndexerDeclarationParameterComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			Assert.AreEqual (@"[int a, int b]", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestWithinIndexerDeclarationBracket ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			Assert.AreEqual (@"[ int a, int b ]", result.GetTextAt (i1, i2 - i1));
		}

		#endregion

		#region Brackets
		
		[Test()]
		public void TestSpacesWithinBrackets ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesWithinBrackets = true;
			policy.SpacesBeforeBrackets = false;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		this[0] = 5;
	}
}");
			Assert.AreEqual (@"class Test
{
	void TestMe ()
	{
		this[ 0 ] = 5;
	}
}", result.Text);
			
			
		}

		[Test()]
		public void TestSpacesBeforeBrackets ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpacesBeforeBrackets = true;
			
			var result = GetResult (policy, @"class Test
{
	void TestMe ()
	{
		this[0] = 5;
	}
}");
			Assert.AreEqual (@"class Test
{
	void TestMe ()
	{
		this [0] = 5;
	}
}", result.Text);
			
			
		}

		[Test()]
		public void TestBeforeBracketComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
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
			Assert.AreEqual (@"[1 ,2 ,3]", result.GetTextAt (i1, i2 - i1));
		}

		[Test()]
		public void TestAfterBracketComma ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceAfterBracketComma = true;
			
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		this[1,2,3] = 5;
	}
}");
			
			int i1 = result.Text.LastIndexOf ("[");
			int i2 = result.Text.LastIndexOf ("]") + "]".Length;
			Assert.AreEqual (@"[1, 2, 3]", result.GetTextAt (i1, i2 - i1));
		}

		#endregion
		
		[Test()]
		public void TestSpacesBeforeArrayDeclarationBrackets ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.SpaceBeforeArrayDeclarationBrackets = true;
			
			var result = GetResult (policy, @"class Test {
	int[] a;
	int[][] b;
}");
			
			Assert.AreEqual (@"class Test
{
	int [] a;
	int [][] b;
}", result.Text);
			
			
		}

		[Test()]
		public void TestRemoveWhitespacesBeforeSemicolon ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			var result = GetResult (policy, @"class Test {
	void TestMe ()
	{
		Foo ()        ;
	}
}");
			int i1 = result.Text.IndexOf ("Foo");
			int i2 = result.Text.LastIndexOf (";") + ";".Length;
			Assert.AreEqual (@"Foo ();", result.GetTextAt (i1, i2 - i1));
		}
		
	}
}
