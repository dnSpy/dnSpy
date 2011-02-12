// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.TypeMembers
{
	[TestFixture, Ignore]
	public class FieldDeclarationTests
	{
		[Test]
		public void SimpleFieldDeclarationTest()
		{
			throw new NotImplementedException();
			/*
			FieldDeclaration fd = ParseUtilCSharp.ParseTypeMember<FieldDeclaration>("int[,,,] myField;");
			Assert.AreEqual("System.Int32", fd.TypeReference.Type);
			Assert.AreEqual(new int[] { 3 } , fd.TypeReference.RankSpecifier);
			Assert.AreEqual(1, fd.Fields.Count);
			
			Assert.AreEqual("myField", ((VariableDeclaration)fd.Fields[0]).Name);*/
		}
		
		// TODO add more tests
	}
}
