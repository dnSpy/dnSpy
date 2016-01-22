using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Tests.FSharpPatterns
{
	[TestFixture]
	public class FSharpPatternTests
	{
		[Test]
		public void FSharpUsingDecompilesToCSharpUsing_Debug()
		{
			var ilCode = TestHelpers.FuzzyReadResource("FSharpUsing.fs.Debug.il");
			var csharpCode = TestHelpers.FuzzyReadResource("FSharpUsing.fs.Debug.cs");
			TestHelpers.RunIL(ilCode, csharpCode);
		}

		[Test]
		public void FSharpUsingDecompilesToCSharpUsing_Release()
		{
			var ilCode = TestHelpers.FuzzyReadResource("FSharpUsing.fs.Release.il");
			var csharpCode = TestHelpers.FuzzyReadResource("FSharpUsing.fs.Release.cs");
			TestHelpers.RunIL(ilCode, csharpCode);
		}
	}
}
