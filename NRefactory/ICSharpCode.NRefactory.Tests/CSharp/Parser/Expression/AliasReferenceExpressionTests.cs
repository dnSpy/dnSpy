// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)


using System;
using System.IO;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class AliasReferenceExpressionTests
	{
		[Test, Ignore]
		public void GlobalReferenceExpressionTest()
		{
			CSharpParser parser = new CSharpParser();
			parser.ParseTypeReference(new StringReader("global::System"));
			//Assert.IsTrue(tre.TypeReference.IsGlobal);
			//Assert.AreEqual("System", tre.TypeReference.Type);
			throw new NotImplementedException();
		}
		
		[Test, Ignore]
		public void GlobalTypeDeclaration()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("global::System.String a;");
			//TypeReference typeRef = lvd.GetTypeForVariable(0);
			//Assert.IsTrue(typeRef.IsGlobal);
			//Assert.AreEqual("System.String", typeRef.Type);
			throw new NotImplementedException();
		}
		
		// TODO: add tests for aliases other than 'global'
	}
}
