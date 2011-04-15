// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class TypeReferenceExpressionTests
	{
		[Test]
		public void GlobalTypeReferenceExpression()
		{
			TypeReferenceExpression tr = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("global::System");
			Assert.IsTrue (tr.IsMatch (new TypeReferenceExpression () {
				Type = new MemberType () {
					Target = new SimpleType ("global"),
					IsDoubleColon = true,
					MemberName = "System"
				}
			}));
		}
		
		[Test, Ignore ("Doesn't work")]
		public void GlobalTypeReferenceExpressionWithoutTypeName()
		{
			TypeReferenceExpression tr = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("global::", true);
			Assert.IsTrue (tr.IsMatch (new TypeReferenceExpression () {
				Type = new MemberType () {
					Target = new SimpleType ("global"),
					IsDoubleColon = true,
				}
			}));
		}
		
		[Test, Ignore("Primitive types as member reference target are not supported yet")]
		public void IntReferenceExpression()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("int.MaxValue");
			Assert.IsTrue (fre.IsMatch (new MemberReferenceExpression () {
				Target = new TypeReferenceExpression () {
					Type = new PrimitiveType("int")
				},
				MemberName = "MaxValue"
			}));
		}
		
	/*	[Test]
		public void StandaloneIntReferenceExpression()
		{
		// doesn't work because a = int; gives a compiler error.
		// But how do we handle this case for code completion?
			TypeReferenceExpression tre = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("int");
			Assert.IsNotNull (tre.Match (new TypeReferenceExpression () {
				Type = new SimpleType ("int")
			}));
		}*/
		
	}
}
