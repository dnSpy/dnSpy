// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.CodeDom;
using System.Collections.Generic;

using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Visitors;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Output.CodeDom.Tests
{
	[TestFixture]
	public class CodeDOMTypeReferenceTest
	{
		[TestAttribute]
		public void InnerClassTypeReferencTest()
		{
			InnerClassTypeReference ictr = new InnerClassTypeReference(
				new TypeReference("OuterClass", new List<TypeReference> { new TypeReference("String") }),
				"InnerClass",
				new List<TypeReference> { new TypeReference("Int32"), new TypeReference("Int64") });
			Assert.AreEqual("OuterClass<String>+InnerClass<Int32,Int64>", ictr.ToString());
			CodeTypeOfExpression result = (CodeTypeOfExpression)new TypeOfExpression(ictr).AcceptVisitor(new CodeDomVisitor(), null);
			Assert.AreEqual("OuterClass`1+InnerClass`2", result.Type.BaseType);
			Assert.AreEqual(3, result.Type.TypeArguments.Count);
		}
	}
}
