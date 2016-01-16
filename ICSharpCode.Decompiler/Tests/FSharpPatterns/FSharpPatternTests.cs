using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static ICSharpCode.Decompiler.Tests.FSharpPatterns.TestHelpers;

namespace ICSharpCode.Decompiler.Tests.FSharpPatterns
{
	[TestFixture]
	public class FSharpPatternTests
	{
		[Test]
		public void FSharpUsingDecompilesToCSharpUsing_Debug()
		{
			var ilCode = FuzzyReadResource("FSharpUsing.fs.Debug.il");
			var csharpCode = FuzzyReadResource("FSharpUsing.fs.Debug.cs");
			RunIL(ilCode, csharpCode);
		}

		[Test]
		public void FSharpUsingDecompilesToCSharpUsing_Release()
		{
			var ilCode = FuzzyReadResource("FSharpUsing.fs.Release.il");
			var csharpCode = FuzzyReadResource("FSharpUsing.fs.Release.cs");
			RunIL(ilCode, csharpCode);
		}
	}
}
