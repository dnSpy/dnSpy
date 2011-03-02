// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)


using System;
using System.IO;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore("Aliases not yet implemented")]
	public class AliasReferenceExpressionTests
	{
		[Test]
		public void GlobalReferenceExpressionTest()
		{
			CSharpParser parser = new CSharpParser();
			AstType type = parser.ParseTypeReference(new StringReader("global::System"));
			Assert.IsNotNull(
				new MemberType {
					Target = new SimpleType("global"),
					IsDoubleColon = true,
					MemberName = "System"
				}.Match(type)
			);
		}
		
		[Test]
		public void GlobalTypeDeclaration()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("global::System.String a;");
			Assert.IsNotNull(
				new VariableDeclarationStatement {
					Type = new MemberType {
						Target = new MemberType {
							Target = new SimpleType("global"),
							IsDoubleColon = true,
							MemberName = "System"
						},
						IsDoubleColon = false,
						MemberName = "String",
					},
					Variables = {
						new VariableInitializer("a")
					}
				}.Match(lvd)
			);
		}
		
		// TODO: add tests for aliases other than 'global'
	}
}
