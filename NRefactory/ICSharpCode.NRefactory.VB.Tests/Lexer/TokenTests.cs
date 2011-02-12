// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.VB.Parser;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Lexer
{
	[TestFixture]
	public class TokenTests
	{
		[Test]
		public void TokenToStringDoesNotThrowException()
		{
			Assert.DoesNotThrow(
				() => {
					string text = new Token(71, 1, 1).ToString();
				}
			);
		}
	}
}
