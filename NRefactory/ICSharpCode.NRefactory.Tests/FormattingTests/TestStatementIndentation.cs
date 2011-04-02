// 
// TestStatementIndentation.cs
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
	public class TestStatementIndentation : TestBase
	{
		[Test()]
		public void TestInvocationIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy,
@"class Test {
	Test TestMethod ()
	{
this.TestMethod ();
	}
}",
@"class Test {
	Test TestMethod ()
	{
		this.TestMethod ();
	}
}");
		}

		[Test()]
		public void TestIndentBlocks ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.IndentBlocks = true;
			
			var adapter = Test (policy,
@"class Test {
	Test TestMethod ()
	{
{
{}
}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		{
			{}
		}
	}
}");
			policy.IndentBlocks = false;
			Continue (policy, adapter, @"class Test
{
	Test TestMethod ()
	{
		{
		{}
		}
	}
}");
		}

		[Test()]
		public void TestBreakIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, 
@"class Test {
	Test TestMethod ()
	{
                              break;
	}
}",
@"class Test {
	Test TestMethod ()
	{
		break;
	}
}");
		}

		[Test()]
		public void TestCheckedIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
checked {
}
	}
}", @"class Test {
	Test TestMethod ()
	{
		checked {
		}
	}
}");
		}

		[Test()]
		public void TestBaseIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
                              base.FooBar();
	}
}", @"class Test {
	Test TestMethod ()
	{
		base.FooBar ();
	}
}");
		}

		[Test()]
		public void TestUncheckedIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
unchecked {
}
	}
}", 
@"class Test {
	Test TestMethod ()
	{
		unchecked {
		}
	}
}");
		}

		[Test()]
		public void TestContinueIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
continue;
	}
}",
@"class Test {
	Test TestMethod ()
	{
		continue;
	}
}");
		}

		[Test()]
		public void TestEmptyStatementIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
;
	}
}", @"class Test {
	Test TestMethod ()
	{
		;
	}
}");
		}

		[Test()]
		public void TestFixedStatementIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
fixed (object* obj = &obj)
;
	}
}",
@"class Test {
	Test TestMethod ()
	{
		fixed (object* obj = &obj)
			;
	}
}");
		}

		[Test()]
		public void TestFixedForcementAdd ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.FixedBraceForcement = BraceForcement.AddBraces;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		fixed (object* obj = &obj) {
		}
		fixed (object* obj = &obj) ;
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		fixed (object* obj = &obj) {
		}
		fixed (object* obj = &obj) {
			;
		}
	}
}");
		}

		[Test()]
		public void TestForeachIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
foreach (var obj in col) {
}
	}
}", @"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col) {
		}
	}
}");
		}

		[Test()]
		public void TestForIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
for (;;) {
}
	}
}", @"class Test {
	Test TestMethod ()
	{
		for (;;) {
		}
	}
}");
		}

		[Test()]
		public void TestGotoIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
goto label;
	}
}",
@"class Test {
	Test TestMethod ()
	{
		goto label;
	}
}");
		}

		[Test()]
		public void TestReturnIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
return;
	}
}",
@"class Test {
	Test TestMethod ()
	{
		return;
	}
}");
		}

		[Test()]
		public void TestLockIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
lock (this) {
}
	}
}",
@"class Test {
	Test TestMethod ()
	{
		lock (this) {
		}
	}
}");
		}

		[Test()]
		public void TestThrowIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
throw new NotSupportedException ();
	}
}",
@"class Test {
	Test TestMethod ()
	{
		throw new NotSupportedException ();
	}
}");
		}

		[Test()]
		public void TestUnsafeIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
unsafe {
}
	}
}",
@"class Test {
	Test TestMethod ()
	{
		unsafe {
		}
	}
}");
		}

		[Test()]
		public void TestUsingIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
using (var o = new MyObj()) {
}
	}
}", @"class Test {
	Test TestMethod ()
	{
		using (var o = new MyObj()) {
		}
	}
}");
		}

		[Test()]
		public void TestUsingForcementAdd ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.UsingBraceForcement = BraceForcement.AddBraces;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		using (var o = new MyObj()) {
		}
		using (var o = new MyObj()) ;
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		using (var o = new MyObj()) {
		}
		using (var o = new MyObj()) {
			;
		}
	}
}");
		}

		[Test()]
		public void TestUsingForcementDoNotChange ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.UsingBraceForcement = BraceForcement.DoNotChange;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		using (var o = new MyObj()) {
		}
		using (var o = new MyObj()) ;
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		using (var o = new MyObj()) {
		}
		using (var o = new MyObj())
			;
	}
}");
		}

		[Test()]
		public void TestUsingAlignment ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AlignEmbeddedUsingStatements = true;
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			var adapter = Test (policy, @"class Test {
	Test TestMethod ()
	{
using (var p = new MyObj())
using (var o = new MyObj()) {
}
	}
}",
@"class Test {
	Test TestMethod ()
	{
		using (var p = new MyObj())
		using (var o = new MyObj()) {
		}
	}
}");
			policy.AlignEmbeddedUsingStatements = false;
			Continue (policy, adapter, @"class Test {
	Test TestMethod ()
	{
		using (var p = new MyObj())
			using (var o = new MyObj()) {
			}
	}
}");
		}

		[Test()]
		public void TestVariableDeclarationIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
Test a;
	}
}",
@"class Test {
	Test TestMethod ()
	{
		Test a;
	}
}");
		}

		[Test()]
		public void TestConstantVariableDeclarationIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
const int a = 5;
	}
}",
@"class Test {
	Test TestMethod ()
	{
		const int a = 5;
	}
}");
		}

		[Test()]
		public void TestYieldIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			Test (policy, @"class Test {
	Test TestMethod ()
	{
yield return null;
	}
}",
@"class Test {
	Test TestMethod ()
	{
		yield return null;
	}
}");
		}

		[Test()]
		public void TestWhileIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test {
	Test TestMethod ()
	{
while (true)
;
	}
}",
@"class Test {
	Test TestMethod ()
	{
		while (true)
			;
	}
}");
		}

		[Test()]
		public void TestDoWhileIndentation ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			Test (policy, @"class Test {
	Test TestMethod ()
	{
do {
} while (true);
	}
}",
@"class Test {
	Test TestMethod ()
	{
		do {
		} while (true);
	}
}");
		}

		[Test()]
		public void TestForeachBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col) {}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col) {
		}
	}
}");
		}

		[Test()]
		public void TestForeachBracketPlacement2 ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLineShifted2;
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col) {;}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col)
			{
				;
			}
	}
}");
		}

		[Test()]
		public void TestForEachBraceForcementAdd ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLine;
			policy.ForEachBraceForcement = BraceForcement.AddBraces;
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col)
		{
		}
		foreach (var obj in col) ;
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col)
		{
		}
		foreach (var obj in col)
		{
			;
		}
	}
}");
		}

		[Test()]
		public void TestForBraceForcementAdd ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLine;
			policy.ForBraceForcement = BraceForcement.AddBraces;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		for (;;)
		{
		}
		for (;;) ;
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		for (;;)
		{
		}
		for (;;)
		{
			;
		}
	}
}");
		}

		[Test()]
		public void TestForEachBraceForcementRemove ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLine;
			policy.ForEachBraceForcement = BraceForcement.RemoveBraces;
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col)
		{
			;
			;
		}
		foreach (var obj in col)
		{
			;
		}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		foreach (var obj in col)
		{
			;
			;
		}
		foreach (var obj in col)
			;
	}
}");
		}

		[Test()]
		public void TestIfBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		if (true) {}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		if (true) {
		}
	}
}");
		}

		[Test()]
		public void TestAllowIfBlockInline ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.AllowIfBlockInline = true;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		if (true) {}
	}
}", @"class Test
{
	Test TestMethod ()
	{
		if (true) {}
	}
}");
			
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		if (true) { Foo (); }
	}
}", @"class Test
{
	Test TestMethod ()
	{
		if (true) { Foo (); }
	}
}");
			
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		if (true) Foo ();
	}
}", @"class Test
{
	Test TestMethod ()
	{
		if (true) Foo ();
	}
}");
			
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		if (true)
			Foo ();
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		if (true)
			Foo ();
	}
}");
		}

		[Test()]
		public void TestIfElseBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		if (true) {} else {}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		if (true) {
		} else {
		}
	}
}");
		}

		[Test()]
		public void TestIfForcementRemove ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLine;
			policy.IfElseBraceForcement = BraceForcement.RemoveBraces;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		if (true)
		{
			;
			;
		}
		if (true)
		{
			;
		}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		if (true)
		{
			;
			;
		}
		if (true)
			;
	}
}");
		}

		[Test()]
		public void TestIfAlignment ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AlignEmbeddedIfStatements = true;
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			var adapter = Test (policy, @"class Test {
	Test TestMethod ()
	{
if (a)
if (b) {
}
	}
}",
@"class Test {
	Test TestMethod ()
	{
		if (a)
		if (b) {
		}
	}
}");
			policy.AlignEmbeddedIfStatements = false;
			Continue (policy, adapter, @"class Test {
	Test TestMethod ()
	{
		if (a)
			if (b) {
			}
	}
}");
		}

		[Test()]
		public void TestIfForcementAdd ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.IfElseBraceForcement = BraceForcement.AddBraces;
			
			Test (policy, @"class Test
{
	void TestMethod ()
	{
		if (true)
			Call ();
	}
}",
@"class Test
{
	void TestMethod ()
	{
		if (true) {
			Call ();
		}
	}
}");
		}

		[Test()]
		public void TestIfForcementWithComment ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.IfElseBraceForcement = BraceForcement.AddBraces;
			
			Test (policy, @"class Test
{
	void TestMethod ()
	{
		if (true) // TestComment
			Call ();
	}
}",
@"class Test
{
	void TestMethod ()
	{
		if (true) {
			// TestComment
			Call ();
		}
	}
}");
		}

		[Test()]
		public void TestIfElseForcementAdd ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.IfElseBraceForcement = BraceForcement.AddBraces;
			
			Test (policy, @"class Test
{
	void TestMethod ()
	{
		if (true)
			Call ();
		else
			Call2 ();
	}
}",
@"class Test
{
	void TestMethod ()
	{
		if (true) {
			Call ();
		} else {
			Call2 ();
		}
	}
}");
		}

		[Test()]
		public void TestIfElseIFForcementAdd ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.IfElseBraceForcement = BraceForcement.AddBraces;
			
			Test (policy, @"class Test
{
	void TestMethod ()
	{
		if (true)
			Call ();
		else if (false)
			Call2 ();
	}
}",
@"class Test
{
	void TestMethod ()
	{
		if (true) {
			Call ();
		} else if (false) {
			Call2 ();
		}
	}
}");
		}

		[Test()]
		public void TestElseOnNewLine ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PlaceElseOnNewLine = true;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		if (true) {
			;
		} else if (false) {
			;
		}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		if (true) {
			;
		}
		else if (false) {
			;
		}
	}
}");
		}

		[Test()]
		public void TestElseIfOnNewLine ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PlaceElseIfOnNewLine = true;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		if (true) {
			;
		} else if (false) {
			;
		}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		if (true) {
			;
		} else
		if (false) {
			;
		}
	}
}");
		}

		[Test()]
		public void TestElseOnNewLineOff ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PlaceElseOnNewLine = false;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		if (true) {
			;
		}
		else if (false) {
			;
		}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		if (true) {
			;
		} else if (false) {
			;
		}
	}
}");
		}

		[Test()]
		public void TestSimpleIfElseComment ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			policy.PlaceElseIfOnNewLine = false; // for simple statements it must be new line.
			
			Test (policy, @"class Test
{
	void TestMethod ()
	{
		if (true) Call (); else Call ();
	}
}",
@"class Test
{
	void TestMethod ()
	{
		if (true)
			Call ();
		else
			Call ();
	}
}");
		}

		[Test()]
		public void TestWhileForcementRemove ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLine;
			policy.WhileBraceForcement = BraceForcement.RemoveBraces;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		while (true)
		{
			;
			;
		}
		while (true)
		{
			;
		}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		while (true)
		{
			;
			;
		}
		while (true)
			;
	}
}");
		}

		[Test()]
		public void TestFixedBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.NextLineShifted;
			policy.FixedBraceForcement = BraceForcement.AddBraces;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		fixed (object* obj = &obj)

;
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		fixed (object* obj = &obj)
			{
			;
			}
	}
}");
		}

		[Test()]
		public void TestForBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLineWithoutSpace;
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		for (;;) {;}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		for (;;){
			;
		}
	}
}");
		}

		[Test()]
		public void TestCheckedBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLineWithoutSpace;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		checked {;}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		checked{
			;
		}
	}
}");
		}

		[Test()]
		public void TestUncheckedBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLineWithoutSpace;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		unchecked {;}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		unchecked{
			;
		}
	}
}");
		}

		[Test()]
		public void TestLockBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		lock (this)
		{
			;
		}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		lock (this) {
			;
		}
	}
}");
		}

		[Test()]
		public void TestUnsafeBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		unsafe
		{
			;
		}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		unsafe {
			;
		}
	}
}");
		}

		[Test()]
		public void TestUsingBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		using (var e = new E())
		{
			;
		}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		using (var e = new E()) {
			;
		}
	}
}");
		}

		[Test()]
		public void TestWhileBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		while (true)
		{
			;
		}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		while (true) {
			;
		}
	}
}");
		}

		[Test()]
		public void TestDoWhileBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		do
		{
			;
		} while (true);
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		do {
			;
		} while (true);
	}
}");
		}

		[Test()]
		public void TestSwitchFormatting1 ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.IndentSwitchBody = true;
			policy.IndentCaseBody = true;
			policy.IndentBreakStatements = true;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		switch (a) { case 1: case 2: DoSomething(); break; default: Foo (); break;}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		switch (a) {
			case 1:
			case 2:
				DoSomething ();
				break;
			default:
				Foo ();
				break;
		}
	}
}");
		}

		[Test()]
		public void TestSwitchFormatting2 ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.IndentSwitchBody = false;
			policy.IndentCaseBody = false;
			policy.IndentBreakStatements = false;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		switch (a) { case 1: case 2: DoSomething(); break; default: Foo (); break;}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		switch (a) {
		case 1:
		case 2:
		DoSomething ();
		break;
		default:
		Foo ();
		break;
		}
	}
}");
		}

		[Test()]
		public void TestTryCatchBracketPlacement ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.StatementBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		try { ; } catch (Exception e) { } finally { }
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		try {
			;
		} catch (Exception e) {
		} finally {
		}
	}
}");
		}

		[Test()]
		public void TestPlaceCatchOnNewLine ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.PlaceCatchOnNewLine = true;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		try {
			;
		} catch (Exception e) {
		} finally {
		}
	}
}",
@"class Test
{
	Test TestMethod ()
	{
		try {
			;
		}
		catch (Exception e) {
		} finally {
		}
	}
}");
		}

		[Test()]
		public void TestPlaceFinallyOnNewLine ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PlaceFinallyOnNewLine = true;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		try {
			;
		} catch (Exception e) {
		} finally {
		}
	}
}",
	@"class Test
{
	Test TestMethod ()
	{
		try {
			;
		} catch (Exception e) {
		}
		finally {
		}
	}
}");
		}

		[Test()]
		public void TestPlaceWhileOnNewLine ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			policy.PlaceWhileOnNewLine = true;
			
			Test (policy, @"class Test
{
	Test TestMethod ()
	{
		do {
			;
		} while (true);
	}
}",
	@"class Test
{
	Test TestMethod ()
	{
		do {
			;
		}
		while (true);
	}
}");
		}
		
	}
}
