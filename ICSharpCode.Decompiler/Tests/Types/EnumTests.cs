using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.Types
{
	public class EnumTests : DecompilerTestBase
	{
		[StaticTestFactory]
		public static IEnumerable<Test> EnumSamples()
		{
			return GenerateSectionTests(@"Types\EnumSamples.cs");
		}
	}
}
