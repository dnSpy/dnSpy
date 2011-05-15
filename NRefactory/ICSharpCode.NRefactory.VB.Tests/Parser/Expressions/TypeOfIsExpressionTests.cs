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
	public class TypeOfIsExpressionTests
	{
		#region VB.NET
		[Test]
		public void VBNetSimpleTypeOfIsExpression()
		{
			TypeOfIsExpression ce = ParseUtil.ParseExpression<TypeOfIsExpression>("TypeOf o Is MyObject");
			Assert.AreEqual("MyObject", ce.TypeReference.Type);
			Assert.IsTrue(ce.Expression is SimpleNameExpression);
		}
		
		[Test]
		public void VBNetGenericTypeOfIsExpression()
		{
			TypeOfIsExpression ce = ParseUtil.ParseExpression<TypeOfIsExpression>("TypeOf o Is List(of T)");
			Assert.AreEqual("List", ce.TypeReference.Type);
			Assert.AreEqual("T", ce.TypeReference.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is SimpleNameExpression);
		}
		#endregion
	}
}
