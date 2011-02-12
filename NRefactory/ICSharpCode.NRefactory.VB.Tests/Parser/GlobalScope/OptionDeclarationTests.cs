// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Dom;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Dom
{
	[TestFixture]
	public class OptionDeclarationTests
	{
		[Test]
		public void VBNetStrictOptionDeclarationTest()
		{
			string program = "Option Strict On\n";
			OptionDeclaration opDec = ParseUtil.ParseGlobal<OptionDeclaration>(program);
			Assert.AreEqual(OptionType.Strict, opDec.OptionType);
			Assert.IsTrue(opDec.OptionValue);
		}
		
		[Test]
		public void VBNetExplicitOptionDeclarationTest()
		{
			string program = "Option Explicit Off\n";
			OptionDeclaration opDec = ParseUtil.ParseGlobal<OptionDeclaration>(program);
			Assert.AreEqual(OptionType.Explicit, opDec.OptionType);
			Assert.IsFalse(opDec.OptionValue, "Off option value excepted!");
		}
		
		[Test]
		public void VBNetCompareBinaryOptionDeclarationTest()
		{
			string program = "Option Compare Binary\n";
			OptionDeclaration opDec = ParseUtil.ParseGlobal<OptionDeclaration>(program);
			Assert.AreEqual(OptionType.CompareBinary, opDec.OptionType);
			Assert.IsTrue(opDec.OptionValue);
		}
		
		[Test]
		public void VBNetCompareTextOptionDeclarationTest()
		{
			string program = "Option Compare Text\n";
			OptionDeclaration opDec = ParseUtil.ParseGlobal<OptionDeclaration>(program);
			Assert.AreEqual(OptionType.CompareText, opDec.OptionType);
			Assert.IsTrue(opDec.OptionValue);
		}

		[Test]
		public void VBNetInferOnOptionDeclarationTest()
		{
			string program = "Option Infer On\n";
			OptionDeclaration opDec = ParseUtil.ParseGlobal<OptionDeclaration>(program);
			Assert.AreEqual(OptionType.Infer, opDec.OptionType);
			Assert.IsTrue(opDec.OptionValue);
		}

		[Test]
		public void VBNetInferOffOptionDeclarationTest()
		{
			string program = "Option Infer\n";
			OptionDeclaration opDec = ParseUtil.ParseGlobal<OptionDeclaration>(program);
			Assert.AreEqual(OptionType.Infer, opDec.OptionType);
			Assert.IsTrue(opDec.OptionValue);
		}
		
		[Test]
		public void VBNetInferOptionDeclarationTest()
		{
			string program = "Option Infer\n";
			OptionDeclaration opDec = ParseUtil.ParseGlobal<OptionDeclaration>(program);
			Assert.AreEqual(OptionType.Infer, opDec.OptionType);
			Assert.IsTrue(opDec.OptionValue);
		}
		
		[Test]
		public void VBNetInvalidOptionDeclarationTest()
		{
			string program = "Option\n";
			VBParser parser = ParserFactory.CreateParser(new StringReader(program));
			parser.Parse();
			Assert.IsFalse(parser.Errors.ErrorOutput.Length == 0, "Expected errors, but operation completed successfully");
		}
	}
}
