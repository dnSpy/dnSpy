using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.Types
{
	[TestFixture]
	public class EnumTests : DecompilerTestBase
	{
		[Test]
		public void EnumSamples()
		{
			ValidateFileRoundtrip(@"Types\S_EnumSamples.cs");
		}
	}
}
