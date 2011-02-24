using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.Types
{
	public class TypeTests : DecompilerTestBase
	{
		[Test]
		public void ValueTypes()
		{
			ValidateFileRoundtrip(@"Types\ValueTypes.cs");
		}

		[Test]
		public void PropertiesAndEvents()
		{
			ValidateFileRoundtrip(@"Types\PropertiesAndEvents.cs");
		}

		[Test]
		public void DelegateConstruction()
		{
			ValidateFileRoundtrip(@"Types\DelegateConstruction.cs");
		}
	}
}
