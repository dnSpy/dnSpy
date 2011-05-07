// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.TypeSystem
{
	[TestFixture]
	public class StructureTests
	{
		[Test]
		public void ClasesThatSupportInterningAreSealed()
		{
			foreach (Type type in typeof(ISupportsInterning).Assembly.GetTypes()) {
				if (typeof(ISupportsInterning).IsAssignableFrom(type) && !type.IsInterface) {
					Assert.IsTrue(type.IsSealed, type.FullName);
				}
			}
		}
	}
}
