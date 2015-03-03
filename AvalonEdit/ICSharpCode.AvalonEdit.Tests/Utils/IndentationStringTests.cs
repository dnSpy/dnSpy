// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
