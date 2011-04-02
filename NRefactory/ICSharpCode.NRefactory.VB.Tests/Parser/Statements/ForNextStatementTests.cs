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
	public class ForNextStatementTests
	{
		#region VB.NET
		[Test]
		public void VBNetForNextStatementTest()
		{
			ForNextStatement forNextStatement = ParseUtil.ParseStatement<ForNextStatement>("For i=0 To 10 Step 2 : Next i");
		}
		
		[Test]
		public void VBNetForNextStatementWithComplexExpressionTest()
		{
			ForNextStatement forNextStatement = ParseUtil.ParseStatement<ForNextStatement>("For SomeMethod().Property = 0 To 10 : Next SomeMethod().Property");
		}
		#endregion
	}
}
