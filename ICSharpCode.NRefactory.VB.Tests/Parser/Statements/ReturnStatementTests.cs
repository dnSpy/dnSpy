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
	public class ReturnStatementTests
	{
		#region VB.NET
		[Test]
		public void VBNetEmptyReturnStatementTest()
		{
			ReturnStatement returnStatement = ParseUtil.ParseStatement<ReturnStatement>("Return");
			Assert.IsTrue(returnStatement.Expression.IsNull);
		}
		
		[Test]
		public void VBNetReturnStatementTest()
		{
			ReturnStatement returnStatement = ParseUtil.ParseStatement<ReturnStatement>("Return 5");
			Assert.IsFalse(returnStatement.Expression.IsNull);
			Assert.IsTrue(returnStatement.Expression is PrimitiveExpression);
		}
		#endregion
	}
}
