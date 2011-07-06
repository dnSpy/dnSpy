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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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

		[Ignore ("TODO")]
		[Test()]
		public void TestIndentBlocks ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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

		[Ignore ("TODO")]
		[Test()]
		public void TestUsingAlignment ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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

		[Ignore ("TODO")]
		[Test()]
		public void TestIfAlignment ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
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
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
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
