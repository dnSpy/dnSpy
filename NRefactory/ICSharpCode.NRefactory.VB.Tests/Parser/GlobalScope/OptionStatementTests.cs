// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Ast;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class OptionStatementTests
	{
		[Test]
		public void InvalidOptionSyntax()
		{
			string program = "Option\n";
			ParseUtil.ParseGlobal<OptionStatement>(program, true);
		}
		
		[Test]
		public void StrictOption()
		{
			string program = "Option Strict On\n";
			
			var node = new OptionStatement {
				OptionType = OptionType.Strict,
				OptionValue = OptionValue.On
			};
			
			ParseUtil.AssertGlobal(program, node);
		}
		
		[Test]
		public void ExplicitOption()
		{
			string program = "Option Explicit Off\n";
			
			var node = new OptionStatement {
				OptionType = OptionType.Explicit,
				OptionValue = OptionValue.Off
			};
			
			ParseUtil.AssertGlobal(program, node);
		}
		
		[Test]
		public void CompareBinaryOption()
		{
			string program = "Option Compare Binary\n";
			
			var node = new OptionStatement {
				OptionType = OptionType.Compare,
				OptionValue = OptionValue.Binary
			};
			
			ParseUtil.AssertGlobal(program, node);
		}

		[Test]
		public void CompareTextOption()
		{
			string program = "Option Compare Text\n";
			
			var node = new OptionStatement {
				OptionType = OptionType.Compare,
				OptionValue = OptionValue.Text
			};
			
			ParseUtil.AssertGlobal(program, node);
		}

		[Test]
		public void InferOnOption()
		{
			string program = "Option Infer On\n";
			
			var node = new OptionStatement {
				OptionType = OptionType.Infer,
				OptionValue = OptionValue.On
			};
			
			ParseUtil.AssertGlobal(program, node);
		}

		[Test]
		public void InferOffOption()
		{
			string program = "Option Infer Off\n";
			
			var node = new OptionStatement {
				OptionType = OptionType.Infer,
				OptionValue = OptionValue.Off
			};
			
			ParseUtil.AssertGlobal(program, node);
		}

		[Test]
		public void InferOption()
		{
			string program = "Option Infer\n";
			
			var node = new OptionStatement {
				OptionType = OptionType.Infer,
				OptionValue = OptionValue.On
			};
			
			ParseUtil.AssertGlobal(program, node);
		}
	}
}
