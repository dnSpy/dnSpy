using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.CustomAttributes
{
	[TestFixture]
	public class CustomAttributeTests : DecompilerTestBase
	{
		[Test]
		public void CustomAttributeSamples()
		{
			ValidateFileRoundtrip(@"CustomAttributes\S_CustomAttributeSamples.cs");
		}

		[Test]
		public void CustomAttributesMultiTest()
		{
			ValidateFileRoundtrip(@"CustomAttributes\S_CustomAttributes.cs");
		}

		[Test]
		public void AssemblyCustomAttributesMultiTest()
		{
			ValidateFileRoundtrip(@"CustomAttributes\S_AssemblyCustomAttribute.cs");
		}
	}
}
