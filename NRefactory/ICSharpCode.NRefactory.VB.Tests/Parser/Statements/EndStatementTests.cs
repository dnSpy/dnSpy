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
	public class EndStatementTests
	{
		#region VB.NET
		[Test]
		public void VBNetEndStatementTest()
		{
			EndStatement endStatement = ParseUtil.ParseStatement<EndStatement>("End");
		}
		
		[Test]
		public void VBNetEndStatementInIfThenTest2()
		{
			IfElseStatement endStatement = ParseUtil.ParseStatement<IfElseStatement>("IF a THEN End");
		}
		#endregion
	}
}
