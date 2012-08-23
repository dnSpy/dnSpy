// 
// TestWrapping.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
	public class TestWrapping : TestBase
	{
		[Test()]
		public void TestInitializerWrapAlways()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.ArrayInitializerWrapping = Wrapping.WrapAlways;
			
			Test(policy, @"class Test
{
	void TestMe ()
	{
		var foo = new [] { 1, 2, 3 };
	}
}",
@"class Test
{
	void TestMe ()
	{
		var foo = new [] {
			1,
			2,
			3
		};
	}
}");
		}

		[Test()]
		public void TestInitializerDoNotWrap()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.ArrayInitializerWrapping = Wrapping.DoNotWrap;

			Test(policy,
@"class Test
{
	void TestMe ()
	{
		var foo = new [] {
			1,
			2,
			3
		};
	}
}", @"class Test
{
	void TestMe ()
	{
		var foo = new [] { 1, 2, 3 };
	}
}");
		}

		[Test()]
		public void TestInitializerBraceStyle()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.ArrayInitializerWrapping = Wrapping.WrapAlways;
			policy.ArrayInitializerBraceStyle = BraceStyle.NextLine;
			
			Test(policy, @"class Test
{
	void TestMe ()
	{
		var foo = new [] {
			1,
			2,
			3
		};
	}
}",
@"class Test
{
	void TestMe ()
	{
		var foo = new []
		{
			1,
			2,
			3
		};
	}
}");
		}

		[Test()]
		public void TestChainedMethodCallWrapping()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.ChainedMethodCallWrapping = Wrapping.WrapAlways;
			
			Test(policy, @"class Test
{
	void TestMe ()
	{
		Foo ().Bar ().             Zoom();
	}
}",
@"class Test
{
	void TestMe ()
	{
		Foo ()
			.Bar ()
			.Zoom ();
	}
}");
		}

		[Test()]
		public void TestChainedMethodCallDoNotWrapWrapping()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.ChainedMethodCallWrapping = Wrapping.DoNotWrap;
			
			Test(policy, @"class Test
{
	void TestMe ()
	{
		Foo ()
			.Bar ()
			.Zoom ();
	}
}",
@"class Test
{
	void TestMe ()
	{
		Foo ().Bar ().Zoom ();
	}
}");
		}

		[Test()]
		public void TestMethodCallArgumentWrapping()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodCallArgumentWrapping = Wrapping.WrapAlways;
			policy.NewLineAferMethodCallOpenParentheses = true;
			policy.MethodCallClosingParenthesesOnNewLine = true;

			Test(policy, @"class Test
{
	void TestMe ()
	{
		Foo (1, 2, 3);
	}
}",
@"class Test
{
	void TestMe ()
	{
		Foo (
			1,
			2,
			3
		);
	}
}");
		}

		[Test()]
		public void TestMethodCallArgumentNoNewLineWrapping()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodCallArgumentWrapping = Wrapping.WrapAlways;
			policy.NewLineAferMethodCallOpenParentheses = false;
			policy.MethodCallClosingParenthesesOnNewLine = false;

			Test(policy, @"class Test
{
	void TestMe ()
	{
		FooBar (1, 2, 3);
	}
}",
@"class Test
{
	void TestMe ()
	{
		FooBar (1,
		        2,
		        3);
	}
}");
		}


		[Test()]
		public void TestMethodCallArgumentDoNotWrapWrapping()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodCallArgumentWrapping = Wrapping.DoNotWrap;
			policy.NewLineAferMethodCallOpenParentheses = true;
			policy.MethodCallClosingParenthesesOnNewLine = true;

			Test(policy, @"class Test
{
	void TestMe ()
	{
		Foo (
			1, 
			2, 
			3
		);
	}
}",
@"class Test
{
	void TestMe ()
	{
		Foo (1, 2, 3);
	}
}");
		}

		
		[Test()]
		public void TestIndexerCallArgumentNoNewLineWrapping()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.IndexerArgumentWrapping = Wrapping.WrapAlways;
			policy.NewLineAferIndexerOpenBracket = true;
			policy.IndexerClosingBracketOnNewLine = true;

			Test(policy, @"class Test
{
	void TestMe ()
	{
		FooBar [1, 2, 3] = 5;
	}
}",
@"class Test
{
	void TestMe ()
	{
		FooBar [
			1,
			2,
			3
		] = 5;
	}
}");
		}

		[Test()]
		public void TestObjectCreationArgumentNoNewLineWrapping()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodCallArgumentWrapping = Wrapping.WrapAlways;
			policy.NewLineAferMethodCallOpenParentheses = true;
			policy.MethodCallClosingParenthesesOnNewLine = true;

			Test(policy, @"class Test
{
	void TestMe ()
	{
		new FooBar (1, 2, 3);
	}
}",
@"class Test
{
	void TestMe ()
	{
		new FooBar (
			1,
			2,
			3
		);
	}
}");
		}

		[Test()]
		public void TestMethodDeclarationParameterNewLineWrapping()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodDeclarationParameterWrapping = Wrapping.WrapAlways;
			policy.NewLineAferMethodDeclarationOpenParentheses = true;
			policy.MethodDeclarationClosingParenthesesOnNewLine = true;

			Test(policy, @"class Test
{
	void TestMe (int i, int j, int k)
	{
	}
}",
@"class Test
{
	void TestMe (
		int i,
		int j,
		int k
	)
	{
	}
}");
		}

		[Test()]
		public void TestMethodDeclarationParameterDoNotChangeCase1()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodDeclarationParameterWrapping = Wrapping.DoNotChange;

			Test(policy, @"class Test
{
	void TestMe (
		int i,
		int j,
		int k
			)
	{
	}
}",
@"class Test
{
	void TestMe (
		int i,
		int j,
		int k
	)
	{
	}
}");
		}

		[Test()]
		public void TestMethodDeclarationParameterDoNotChangeCase2()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodDeclarationParameterWrapping = Wrapping.DoNotChange;

			Test(policy, @"class Test
{
	void TestMe (
		int i,
		int j,
		int k						)
	{
	}
}",
@"class Test
{
	void TestMe (
		int i,
		int j,
		int k)
	{
	}
}");
		}

		[Test()]
		public void TestMethodDeclarationParameterDoNotChangeCase3()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodDeclarationParameterWrapping = Wrapping.DoNotChange;

			Test(policy, @"class Test
{
	void TestMe (int i,
	             int j,
	             int k						)
	{
	}
}",
@"class Test
{
	void TestMe (int i,
	             int j,
	             int k)
	{
	}
}");
		}

		[Test()]
		public void TestOperatorDeclarationParameterNewLineWrapping()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodDeclarationParameterWrapping = Wrapping.WrapAlways;
			policy.NewLineAferMethodDeclarationOpenParentheses = true;
			policy.MethodDeclarationClosingParenthesesOnNewLine = false;

			Test(policy, @"class Test
{
	public static Test operator + (Test a, Test b)
	{
		return null;
	}
}",
@"class Test
{
	public static Test operator + (
		Test a,
		Test b)
	{
		return null;
	}
}");
		}

		[Test()]
		public void TestConstructorDeclarationParameterNewLineWrapping()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodDeclarationParameterWrapping = Wrapping.WrapAlways;
			policy.NewLineAferMethodDeclarationOpenParentheses = true;
			policy.MethodDeclarationClosingParenthesesOnNewLine = true;

			Test(policy, @"class Test
{
	Test (int i, int j, int k)
	{
	}
}",
@"class Test
{
	Test (
		int i,
		int j,
		int k
	)
	{
	}
}");
		}

		[Test()]
		public void TestIndexerDeclarationParameterNewLineWrapping()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.IndexerDeclarationParameterWrapping = Wrapping.WrapAlways;
			policy.NewLineAferIndexerDeclarationOpenBracket = true;
			policy.IndexerDeclarationClosingBracketOnNewLine = true;
			Test(policy, @"class Test
{
	int this [int i, int j, int k] {
		get {
		}
	}
}",
@"class Test
{
	int this [
		int i,
		int j,
		int k
	] {
		get {
		}
	}
}");
		}


		[Test()]
		public void TestMethodCallArgumentWrappingDoNotChangeCase1()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodCallArgumentWrapping = Wrapping.DoNotChange;
			policy.NewLineAferMethodCallOpenParentheses = true;
			policy.MethodCallClosingParenthesesOnNewLine = true;

			Test(policy, @"class Test
{
	void TestMe ()
	{
		Foo (
			1,
			2,
			3
		);
	}
}",
@"class Test
{
	void TestMe ()
	{
		Foo (
			1,
			2,
			3
		);
	}
}");
		}

		[Test()]
		public void TestMethodCallArgumentWrappingDoNotChangeCase2()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodCallArgumentWrapping = Wrapping.DoNotChange;
			policy.NewLineAferMethodCallOpenParentheses = true;
			policy.MethodCallClosingParenthesesOnNewLine = true;

			Test(policy, @"class Test
{
	void TestMe ()
	{
		Foo (1,
		     2,
		     3
					);
	}
}",
@"class Test
{
	void TestMe ()
	{
		Foo (1,
		     2,
		     3
		);
	}
}");
		}

		[Test()]
		public void TestMethodCallArgumentWrappingDoNotChangeCase3()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.MethodCallArgumentWrapping = Wrapping.DoNotChange;
			policy.NewLineAferMethodCallOpenParentheses = true;
			policy.MethodCallClosingParenthesesOnNewLine = true;

			Test(policy, @"class Test
{
	void TestMe ()
	{
		Foo (1,
		     2,
		     3           );
	}
}",
@"class Test
{
	void TestMe ()
	{
		Foo (1,
		     2,
		     3);
	}
}");
		}
		
		[Test()]
		public void TestNoBlankLinesBetweenEndBraceAndEndParenthesis ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.BlankLinesBetweenMembers = 1;
			
			var adapter = Test (policy, @"class Test
{
	int Foo (int i, double d, Action a)
	{
		a ();
	}

	void Bar ()
	{
		Foo (1, 2, () => {
		});
	}
}",
@"class Test
{
	int Foo (int i, double d, Action a)
	{
		a ();
	}

	void Bar ()
	{
		Foo (1, 2, () => {
		});
	}
}", FormattingMode.Intrusive);
		}

	}
}
