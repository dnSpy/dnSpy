// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Utils
{
	[TestFixture]
	public class ExtensionMethodsTests
	{
		[Test]
		public void ZeroIsNotCloseToOne()
		{
			Assert.IsFalse(0.0.IsClose(1));
		}
		
		[Test]
		public void ZeroIsCloseToZero()
		{
			Assert.IsTrue(0.0.IsClose(0));
		}
		
		[Test]
		public void InfinityIsCloseToInfinity()
		{
			Assert.IsTrue(double.PositiveInfinity.IsClose(double.PositiveInfinity));
		}
		
		[Test]
		public void NaNIsNotCloseToNaN()
		{
			Assert.IsFalse(double.NaN.IsClose(double.NaN));
		}
	}
}
