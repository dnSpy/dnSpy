// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.GeneralScope
{
	[TestFixture]
	public class CommentTests
	{
		[Test]
		public void SimpleComment()
		{
			string program = @"namespace NS {
	// Comment
}";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			var c = ns.GetChildrenByRole(Roles.Comment).Single();
			Assert.AreEqual(CommentType.SingleLine, c.CommentType);
			Assert.AreEqual(" Comment", c.Content);
		}
		
		[Test]
		public void CStyleComment()
		{
			string program = @"namespace NS {
	/* Comment */
}";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			var c = ns.GetChildrenByRole(Roles.Comment).Single();
			Assert.AreEqual(CommentType.MultiLine, c.CommentType);
			Assert.AreEqual(" Comment ", c.Content);
		}
		
		[Test]
		public void DocumentationComment()
		{
			string program = @"namespace NS {
	/// Comment
}";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			var c = ns.GetChildrenByRole(Roles.Comment).Single();
			Assert.AreEqual(CommentType.Documentation, c.CommentType);
			Assert.AreEqual(" Comment", c.Content);
		}
		
		[Test, Ignore("Parser bug")]
		public void SimpleCommentWith4Slashes()
		{
			string program = @"namespace NS {
	//// Comment
}";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			var c = ns.GetChildrenByRole(Roles.Comment).Single();
			Assert.AreEqual(CommentType.SingleLine, c.CommentType);
			Assert.AreEqual("// Comment", c.Content);
		}
		
		[Test]
		public void MultilineDocumentationComment()
		{
			string program = @"namespace NS {
	/** Comment */
}";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			var c = ns.GetChildrenByRole(Roles.Comment).Single();
			Assert.AreEqual(CommentType.MultiLineDocumentation, c.CommentType);
			Assert.AreEqual(" Comment ", c.Content);
		}
		
		[Test]
		public void EmptyMultilineCommnet()
		{
			string program = @"namespace NS {
	/**/
}";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			var c = ns.GetChildrenByRole(Roles.Comment).Single();
			Assert.AreEqual(CommentType.MultiLine, c.CommentType);
			Assert.AreEqual("", c.Content);
		}
		
		[Test]
		public void MultilineCommentWith3Stars()
		{
			string program = @"namespace NS {
	/*** Comment */
}";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			var c = ns.GetChildrenByRole(Roles.Comment).Single();
			Assert.AreEqual(CommentType.MultiLine, c.CommentType);
			Assert.AreEqual("** Comment ", c.Content);
		}
	}
}
