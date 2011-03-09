using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.Types
{
	[TestFixture]
	public class TypeTests : DecompilerTestBase
	{
		[Test]
		public void TypeMemberDeclarations()
		{
			ValidateFileRoundtrip(@"Types\S_TypeMemberDeclarations.cs");
		}
	}
}
