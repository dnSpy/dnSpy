// 
// TastBlankLineFormatting.cs
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
	public class TestBlankLineFormatting : TestBase
	{
		[Test()]
		public void TestBlankLinesAfterUsings ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.BlankLinesAfterUsings = 2;
			
			var adapter = Test (policy, @"using System;
using System.Text;
namespace Test
{
}",
@"using System;
using System.Text;


namespace Test
{
}", FormattingMode.Intrusive);
			
			policy.BlankLinesAfterUsings = 0;
			Continue (policy, adapter,
@"using System;
using System.Text;
namespace Test
{
}", FormattingMode.Intrusive);
		}

		[Ignore()]
		[Test()]
		public void TestBlankLinesBeforeUsings ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.BlankLinesAfterUsings = 0;
			policy.BlankLinesBeforeUsings = 2;
			
			var adapter = Test (policy, @"using System;
using System.Text;
namespace Test
{
}",
@"

using System;
using System.Text;
namespace Test
{
}", FormattingMode.Intrusive);
			
			policy.BlankLinesBeforeUsings = 0;
			Continue (policy, adapter, 
@"using System;
using System.Text;
namespace Test
{
}", FormattingMode.Intrusive);
		}

		[Test()]
		public void TestBlankLinesBeforeFirstDeclaration ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.BlankLinesBeforeFirstDeclaration = 2;
			
			var adapter = Test (policy, @"namespace Test
{
	class Test
	{
	}
}",
@"namespace Test
{


	class Test
	{
	}
}", FormattingMode.Intrusive);
			
			policy.BlankLinesBeforeFirstDeclaration = 0;
			Continue (policy, adapter,
@"namespace Test
{
	class Test
	{
	}
}", FormattingMode.Intrusive);
		}

		[Test()]
		public void TestBlankLinesBetweenTypes ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.BlankLinesBetweenTypes = 1;
			
			var adapter = Test (policy, @"namespace Test
{
	class Test1
	{
	}
	class Test2
	{
	}
	class Test3
	{
	}
}",
@"namespace Test
{
	class Test1
	{
	}

	class Test2
	{
	}

	class Test3
	{
	}
}", FormattingMode.Intrusive);
			
			policy.BlankLinesBetweenTypes = 0;
			Continue (policy, adapter, @"namespace Test
{
	class Test1
	{
	}
	class Test2
	{
	}
	class Test3
	{
	}
}", FormattingMode.Intrusive);
		}

		[Test()]
		public void TestBlankLinesBetweenFields ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.BlankLinesBetweenFields = 1;
			
			var adapter = Test (policy, @"class Test
{
	int a;
	int b;
	int c;
}",
@"class Test
{
	int a;

	int b;

	int c;
}", FormattingMode.Intrusive);
			
			policy.BlankLinesBetweenFields = 0;
			Continue (policy, adapter, @"class Test
{
	int a;
	int b;
	int c;
}", FormattingMode.Intrusive);
		}

		[Test()]
		public void TestBlankLinesBetweenEventFields ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.BlankLinesBetweenEventFields = 1;
			
			var adapter = Test (policy, @"class Test
{
	public event EventHandler a;
	public event EventHandler b;
	public event EventHandler c;
}",
@"class Test
{
	public event EventHandler a;

	public event EventHandler b;

	public event EventHandler c;
}", FormattingMode.Intrusive);
			
			policy.BlankLinesBetweenEventFields = 0;
			Continue (policy, adapter,
@"class Test
{
	public event EventHandler a;
	public event EventHandler b;
	public event EventHandler c;
}", FormattingMode.Intrusive);
		}

		[Test()]
		public void TestBlankLinesBetweenMembers ()
		{
			CSharpFormattingOptions policy = FormattingOptionsFactory.CreateMono ();
			policy.BlankLinesBetweenMembers = 1;
			
			var adapter = Test (policy, @"class Test
{
	void AMethod ()
	{
	}
	void BMethod ()
	{
	}
	void CMethod ()
	{
	}
}", @"class Test
{
	void AMethod ()
	{
	}

	void BMethod ()
	{
	}

	void CMethod ()
	{
	}
}", FormattingMode.Intrusive);
			
			policy.BlankLinesBetweenMembers = 0;
			Continue (policy, adapter, @"class Test
{
	void AMethod ()
	{
	}
	void BMethod ()
	{
	}
	void CMethod ()
	{
	}
}", FormattingMode.Intrusive);
		}
		
		
		
	}
}

