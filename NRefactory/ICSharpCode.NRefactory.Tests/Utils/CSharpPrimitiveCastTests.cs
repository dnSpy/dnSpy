// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.Utils
{
	[TestFixture]
	public class CSharpPrimitiveCastTests
	{
		// I know, these tests aren't really clever, more of a way to fake code coverage...
		// Well, at least they should ensure the 'tables' in CSharpPrimitiveCast don't contain any typos.
		
		[Test]
		public void FloatToInteger()
		{
			for (int checkedMode = 0; checkedMode < 2; checkedMode++) {
				for (TypeCode to = TypeCode.Char; to <= TypeCode.UInt64; to++) {
					object val = CSharpPrimitiveCast.Cast(to, 3.9f, checkedMode == 1);
					Assert.AreEqual(to, Type.GetTypeCode(val.GetType()));
					Assert.AreEqual(3, Convert.ToInt64(val));
				}
			}
		}
		
		[Test]
		public void DoubleToInteger()
		{
			for (int checkedMode = 0; checkedMode < 2; checkedMode++) {
				for (TypeCode to = TypeCode.Char; to <= TypeCode.UInt64; to++) {
					object val = CSharpPrimitiveCast.Cast(to, 3.9, checkedMode == 1);
					Assert.AreEqual(to, Type.GetTypeCode(val.GetType()));
					Assert.AreEqual(3, Convert.ToInt64(val));
				}
			}
		}
		
		[Test]
		public void DecimalToInteger()
		{
			for (int checkedMode = 0; checkedMode < 2; checkedMode++) {
				for (TypeCode to = TypeCode.Char; to <= TypeCode.UInt64; to++) {
					object val = CSharpPrimitiveCast.Cast(to, 3.9m, checkedMode == 1);
					Assert.AreEqual(to, Type.GetTypeCode(val.GetType()));
					Assert.AreEqual(3, Convert.ToInt64(val));
				}
			}
		}
		
		[Test]
		public void IntegerToInteger()
		{
			for (int checkedMode = 0; checkedMode < 2; checkedMode++) {
				for (TypeCode to = TypeCode.Char; to <= TypeCode.UInt64; to++) {
					for (TypeCode to2 = TypeCode.Char; to2 <= TypeCode.Decimal; to2++) {
						object val = CSharpPrimitiveCast.Cast(to, 3, checkedMode == 1);
						Assert.AreEqual(to, Type.GetTypeCode(val.GetType()));
						Assert.AreEqual(3, Convert.ToInt64(val));
						object val2 = CSharpPrimitiveCast.Cast(to2, val, checkedMode == 1);
						Assert.AreEqual(to2, Type.GetTypeCode(val2.GetType()));
						Assert.AreEqual(3, Convert.ToInt64(val2));
					}
				}
			}
		}
	}
}
