// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class BlockStatementTests
	{
		[Test]
		public void BlockStatementTest()
		{
			BlockStatement blockStmt = ParseUtilCSharp.ParseStatement<BlockStatement>("{}");
			Assert.AreEqual(0, blockStmt.Statements.Count());
		}
		
		[Test, Ignore("position isn't correct when only parsing a block")]
		public void ComplexBlockStatementPositionTest()
		{
			string code = @"{
	WebClient wc = new WebClient();
	wc.Test();
	wc.UploadStringCompleted += delegate {
		output.BeginInvoke((MethodInvoker)delegate {
		                   	output.Text += newText;
		                   });
	};
}";
			BlockStatement blockStmt = ParseUtilCSharp.ParseStatement<BlockStatement>(code);
			Assert.AreEqual(1, blockStmt.StartLocation.Column);
			Assert.AreEqual(1, blockStmt.StartLocation.Line);
			Assert.AreEqual(2, blockStmt.EndLocation.Column);
			Assert.AreEqual(9, blockStmt.EndLocation.Line);
			
			Assert.AreEqual(3, blockStmt.Statements.Count());
		}
	}
}
