using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.CustomAttributes
{
	public class CustomAttributeTests : DecompilerTestBase
	{
		[StaticTestFactory]
		public static IEnumerable<Test> CustomAttributeSamples()
		{			
			return GenerateSectionTests(@"CustomAttributes\S_CustomAttributeSamples.cs");
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
