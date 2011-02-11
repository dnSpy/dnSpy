// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.VB.Dom;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Dom
{
	[TestFixture]
	public class DelegateDeclarationTests
	{
		void TestDelegateDeclaration(DelegateDeclaration dd)
		{
			Assert.AreEqual("System.Void", dd.ReturnType.Type);
			Assert.AreEqual("MyDelegate", dd.Name);
		}
		
		void TestParameters(DelegateDeclaration dd)
		{
			Assert.AreEqual(3, dd.Parameters.Count);
			
			Assert.AreEqual("a", ((ParameterDeclarationExpression)dd.Parameters[0]).ParameterName);
			Assert.AreEqual("System.Int32", ((ParameterDeclarationExpression)dd.Parameters[0]).TypeReference.Type);
			
			Assert.AreEqual("secondParam", ((ParameterDeclarationExpression)dd.Parameters[1]).ParameterName);
			Assert.AreEqual("System.Int32", ((ParameterDeclarationExpression)dd.Parameters[1]).TypeReference.Type);
			
			Assert.AreEqual("lastParam", ((ParameterDeclarationExpression)dd.Parameters[2]).ParameterName);
			Assert.AreEqual("MyObj", ((ParameterDeclarationExpression)dd.Parameters[2]).TypeReference.Type);
		}
		
		#region VB.NET
		[Test]
		public void SimpleVBNetDelegateDeclarationTest()
		{
			string program = "Public Delegate Sub MyDelegate(ByVal a As Integer, ByVal secondParam As Integer, ByVal lastParam As MyObj)\n";
			TestDelegateDeclaration(ParseUtil.ParseGlobal<DelegateDeclaration>(program));
		}
		#endregion
	}
}
