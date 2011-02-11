// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.TypeMembers
{
	[TestFixture]
	public class OperatorDeclarationTests
	{
		[Test, Ignore("type references not yet implemented")]
		public void ImplictOperatorDeclarationTest()
		{
			OperatorDeclaration od = ParseUtilCSharp.ParseTypeMember<OperatorDeclaration>("public static implicit operator double(MyObject f)  { return 0.5d; }");
			Assert.AreEqual(OperatorType.Implicit, od.OperatorType);
			Assert.AreEqual(1, od.Parameters.Count());
			Assert.AreEqual("System.Double", od.ReturnType);
			Assert.AreEqual("op_Implicit", od.Name);
		}
		
		[Test, Ignore("type references not yet implemented")]
		public void ExplicitOperatorDeclarationTest()
		{
			OperatorDeclaration od = ParseUtilCSharp.ParseTypeMember<OperatorDeclaration>("public static explicit operator double(MyObject f)  { return 0.5d; }");
			Assert.AreEqual(OperatorType.Explicit, od.OperatorType);
			Assert.AreEqual(1, od.Parameters.Count());
			Assert.AreEqual("System.Double", od.ReturnType);
			Assert.AreEqual("op_Explicit", od.Name);
		}
		
		[Test, Ignore("type references not yet implemented")]
		public void BinaryPlusOperatorDeclarationTest()
		{
			OperatorDeclaration od = ParseUtilCSharp.ParseTypeMember<OperatorDeclaration>("public static MyObject operator +(MyObject a, MyObject b)  {}");
			Assert.AreEqual(OperatorType.Addition, od.OperatorType);
			Assert.AreEqual(2, od.Parameters.Count());
			Assert.AreEqual("MyObject", od.ReturnType);
			Assert.AreEqual("op_Addition", od.Name);
		}
		
		[Test, Ignore("type references not yet implemented")]
		public void UnaryPlusOperatorDeclarationTest()
		{
			OperatorDeclaration od = ParseUtilCSharp.ParseTypeMember<OperatorDeclaration>("public static MyObject operator +(MyObject a)  {}");
			Assert.AreEqual(OperatorType.UnaryPlus, od.OperatorType);
			Assert.AreEqual(1, od.Parameters.Count());
			Assert.AreEqual("MyObject", od.ReturnType);
			Assert.AreEqual("op_UnaryPlus", od.Name);
		}
	}
}
