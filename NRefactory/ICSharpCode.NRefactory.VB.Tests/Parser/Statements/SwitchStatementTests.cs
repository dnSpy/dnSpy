// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class SwitchStatementTests
	{
		#region VB.NET
		[Test]
		public void VBSwitchStatementTest()
		{
			SwitchStatement switchStmt = ParseUtil.ParseStatement<SwitchStatement>("Select Case a\n Case 4, 5\n Case 6\n Case Else\n End Select");
			Assert.AreEqual("a", ((SimpleNameExpression)switchStmt.SwitchExpression).Identifier);
			// TODO: Extend test
		}
		
		[Test]
		public void InvalidVBSwitchStatementTest()
		{
			SwitchStatement switchStmt = ParseUtil.ParseStatement<SwitchStatement>("Select Case a\n Case \n End Select", true);
			Assert.AreEqual("a", ((SimpleNameExpression)switchStmt.SwitchExpression).Identifier);
			SwitchSection sec = switchStmt.SwitchSections[0];
			Assert.AreEqual(0, sec.SwitchLabels.Count);
		}
		#endregion
	}
}
