using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static ICSharpCode.Decompiler.Tests.FS2CS.TestRunner;

namespace ICSharpCode.Decompiler.Tests.FS2CS
{
	[TestFixture]
	public class FSharpPatternTests
	{
		[Test]
		public void FSharpUsingDecompilesToCSharpUsing_Debug()
		{
			var fsharpCode = FuzzyReadResource("FSharpUsing.fs");
			var csharpCode = FuzzyReadResource("FSharpUsing.fs.Debug.cs");
			Run(fsharpCode, csharpCode, false);
		}

		[Test]
		public void FSharpUsingDecompilesToCSharpUsing_Release()
		{
			var fsharpCode = FuzzyReadResource("FSharpUsing.fs");
			var csharpCode = FuzzyReadResource("FSharpUsing.fs.Release.cs");
			Run(fsharpCode, csharpCode, true);
		}
	}
}
