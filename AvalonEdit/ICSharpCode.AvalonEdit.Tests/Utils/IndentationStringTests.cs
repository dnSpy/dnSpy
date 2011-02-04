// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Utils
{
	[TestFixture]
	public class IndentationStringTests
	{
		[Test]
		public void IndentWithSingleTab()
		{
			var options = new TextEditorOptions { IndentationSize = 4, ConvertTabsToSpaces = false };
			Assert.AreEqual("\t", options.IndentationString);
			Assert.AreEqual("\t", options.GetIndentationString(2));
			Assert.AreEqual("\t", options.GetIndentationString(3));
			Assert.AreEqual("\t", options.GetIndentationString(4));
			Assert.AreEqual("\t", options.GetIndentationString(5));
			Assert.AreEqual("\t", options.GetIndentationString(6));
		}
		
		[Test]
		public void IndentWith4Spaces()
		{
			var options = new TextEditorOptions { IndentationSize = 4, ConvertTabsToSpaces = true };
			Assert.AreEqual("    ", options.IndentationString);
			Assert.AreEqual("   ", options.GetIndentationString(2));
			Assert.AreEqual("  ", options.GetIndentationString(3));
			Assert.AreEqual(" ", options.GetIndentationString(4));
			Assert.AreEqual("    ", options.GetIndentationString(5));
			Assert.AreEqual("   ", options.GetIndentationString(6));
		}
	}
}
