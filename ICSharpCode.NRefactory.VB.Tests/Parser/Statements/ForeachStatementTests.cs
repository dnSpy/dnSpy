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
	public class ForeachStatementTests
	{
		#region VB.NET
		[Test]
		public void VBNetForeachStatementTest()
		{
			ForeachStatement foreachStmt = ParseUtil.ParseStatement<ForeachStatement>("For Each i As Integer In myColl : Next");
			// TODO : Extend test.
		}
		#endregion
		
	}
}
