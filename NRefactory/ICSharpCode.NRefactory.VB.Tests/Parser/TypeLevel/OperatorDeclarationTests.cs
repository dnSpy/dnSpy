// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.VB.Ast;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class OperatorDeclarationTests
	{
		#region VB.NET
		
		[Test]
		public void VBNetImplictOperatorDeclarationTest()
		{
			string programm = @"Public Shared Operator + (ByVal v As Complex) As Complex
					Return v
				End Operator";
			
			OperatorDeclaration od = ParseUtil.ParseTypeMember<OperatorDeclaration>(programm);
			Assert.IsFalse(od.IsConversionOperator);
			Assert.AreEqual(1, od.Parameters.Count);
			Assert.AreEqual(ConversionType.None, od.ConversionType);
			Assert.AreEqual("Complex", od.TypeReference.Type);
		}
		#endregion 
	}
}
