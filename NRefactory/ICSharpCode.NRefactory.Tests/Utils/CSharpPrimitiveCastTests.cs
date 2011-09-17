// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
