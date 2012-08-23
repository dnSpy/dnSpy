//
// MoveToOuterScopeTests.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class MoveToOuterScopeTests : ContextActionTestBase
	{
		void TestStatements (string input, string output) 
		{
			Test<MoveToOuterScopeAction>(@"
class A
{
	void F()
	{"
	 + input + 
@"	}
}", @"
class A
{
	void F()
	{"
     + output + 
@"	}
}");
		}

		[Test]
		public void SimpleCase()
		{
			TestStatements(@"
while (true) {
	int $i = 2;
}
", @"
int i = 2;
while (true) {
}
");
		}
		
		[Test]
		public void IgnoresDeclarationsDirectlyInABody()
		{
			TestWrongContext<MoveToOuterScopeAction>(@"
class A
{
	void F()
	{
		int $i = 2;
	}
}");
		}

		[Test]
		public void MovesOnlyTheCurrentVariableInitialization()
		{
			TestStatements(@"
while (true) {
	int $i = 2, j = 3;
}
", @"
int i = 2;
while (true) {
	int j = 3;
}
");
		}
		
		[Test]
		public void MovesAllInitializersWhenOnType()
		{
			TestStatements(@"
while (true) {
	i$nt i = 2, j = 3;
}
", @"
int i = 2, j = 3;
while (true) {
}
");
		}
		
		[Test]
		public void OnlyMovesDeclarationWhenInitializerDependsOnOtherStatements()
		{
			TestStatements(@"
while (true) {
	int i = 2;
	int j$ = i;
}
", @"
int j;
while (true) {
	int i = 2;
	j = i;
}
");
		}
		
		[Test]
		public void HandlesLambdaDelegate()
		{
			TestStatements(@"
var action = new Action<int>(i => {
	int j$ = 2;
});
", @"
int j = 2;
var action = new Action<int>(i => {
});
");
		}
	}
}

