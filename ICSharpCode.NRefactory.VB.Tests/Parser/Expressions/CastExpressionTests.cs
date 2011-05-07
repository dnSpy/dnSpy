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
	public class CastExpressionTests
	{
		#region VB.NET
		void TestSpecializedCast(string castExpression, Type castType)
		{
			CastExpression ce = ParseUtil.ParseExpression<CastExpression>(castExpression);
			Assert.AreEqual(castType.FullName, ce.CastTo.Type);
			Assert.IsTrue(ce.Expression is SimpleNameExpression);
			Assert.AreEqual(CastType.PrimitiveConversion, ce.CastType);
		}
		
		
		[Test]
		public void VBNetSimpleCastExpression()
		{
			CastExpression ce = ParseUtil.ParseExpression<CastExpression>("CType(o, MyObject)");
			Assert.AreEqual("MyObject", ce.CastTo.Type);
			Assert.IsTrue(ce.Expression is SimpleNameExpression);
			Assert.AreEqual(CastType.Conversion, ce.CastType);
		}
		
		[Test]
		public void VBNetGenericCastExpression()
		{
			CastExpression ce = ParseUtil.ParseExpression<CastExpression>("CType(o, List(of T))");
			Assert.AreEqual("List", ce.CastTo.Type);
			Assert.AreEqual("T", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is SimpleNameExpression);
			Assert.AreEqual(CastType.Conversion, ce.CastType);
		}
		
		[Test]
		public void VBNetSimpleDirectCastExpression()
		{
			CastExpression ce = ParseUtil.ParseExpression<CastExpression>("DirectCast(o, MyObject)");
			Assert.AreEqual("MyObject", ce.CastTo.Type);
			Assert.IsTrue(ce.Expression is SimpleNameExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void VBNetGenericDirectCastExpression()
		{
			CastExpression ce = ParseUtil.ParseExpression<CastExpression>("DirectCast(o, List(of T))");
			Assert.AreEqual("List", ce.CastTo.Type);
			Assert.AreEqual("T", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is SimpleNameExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void VBNetSimpleTryCastExpression()
		{
			CastExpression ce = ParseUtil.ParseExpression<CastExpression>("TryCast(o, MyObject)");
			Assert.AreEqual("MyObject", ce.CastTo.Type);
			Assert.IsTrue(ce.Expression is SimpleNameExpression);
			Assert.AreEqual(CastType.TryCast, ce.CastType);
		}
		
		[Test]
		public void VBNetGenericTryCastExpression()
		{
			CastExpression ce = ParseUtil.ParseExpression<CastExpression>("TryCast(o, List(of T))");
			Assert.AreEqual("List", ce.CastTo.Type);
			Assert.AreEqual("T", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is SimpleNameExpression);
			Assert.AreEqual(CastType.TryCast, ce.CastType);
		}
		
		[Test]
		public void VBNetSpecializedBoolCastExpression()
		{
			TestSpecializedCast("CBool(o)", typeof(System.Boolean));
		}
		
		[Test]
		public void VBNetSpecializedCharCastExpression()
		{
			TestSpecializedCast("CChar(o)", typeof(System.Char));
		}
		
		
		[Test]
		public void VBNetSpecializedStringCastExpression()
		{
			TestSpecializedCast("CStr(o)", typeof(System.String));
		}
		
		[Test]
		public void VBNetSpecializedDateTimeCastExpression()
		{
			TestSpecializedCast("CDate(o)", typeof(System.DateTime));
		}
		
		[Test]
		public void VBNetSpecializedDecimalCastExpression()
		{
			TestSpecializedCast("CDec(o)", typeof(System.Decimal));
		}
		
		[Test]
		public void VBNetSpecializedSingleCastExpression()
		{
			TestSpecializedCast("CSng(o)", typeof(System.Single));
		}
		
		[Test]
		public void VBNetSpecializedDoubleCastExpression()
		{
			TestSpecializedCast("CDbl(o)", typeof(System.Double));
		}
		
		[Test]
		public void VBNetSpecializedByteCastExpression()
		{
			TestSpecializedCast("CByte(o)", typeof(System.Byte));
		}
		
		[Test]
		public void VBNetSpecializedInt16CastExpression()
		{
			TestSpecializedCast("CShort(o)", typeof(System.Int16));
		}
		
		[Test]
		public void VBNetSpecializedInt32CastExpression()
		{
			TestSpecializedCast("CInt(o)", typeof(System.Int32));
		}
		
		[Test]
		public void VBNetSpecializedInt64CastExpression()
		{
			TestSpecializedCast("CLng(o)", typeof(System.Int64));
		}
		
		[Test]
		public void VBNetSpecializedSByteCastExpression()
		{
			TestSpecializedCast("CSByte(o)", typeof(System.SByte));
		}
		
		[Test]
		public void VBNetSpecializedUInt16CastExpression()
		{
			TestSpecializedCast("CUShort(o)", typeof(System.UInt16));
		}
		
		[Test]
		public void VBNetSpecializedUInt32CastExpression()
		{
			TestSpecializedCast("CUInt(o)", typeof(System.UInt32));
		}
		
		[Test]
		public void VBNetSpecializedUInt64CastExpression()
		{
			TestSpecializedCast("CULng(o)", typeof(System.UInt64));
		}
		
		
		[Test]
		public void VBNetSpecializedObjectCastExpression()
		{
			TestSpecializedCast("CObj(o)", typeof(System.Object));
		}
		#endregion
	}
}
