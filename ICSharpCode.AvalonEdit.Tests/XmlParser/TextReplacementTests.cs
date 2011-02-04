// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Xml;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Xml
{
	[TestFixture]
	public class TextReplacementTests
	{
		#region Test Data
		string initialDocumentText = @"<UserControl x:Class='ICSharpCode.Profiler.Controls.TimeLineCell'
	xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
	xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<Grid>
		
	</Grid>
</UserControl>";
		
		string finalDocumentText = @"<UserControl x:Class='ICSharpCode.Profiler.Controls.TimeLineCell'
	xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
	xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
			<Grid>
				<Grid.RowDefinitions>
					<RowDefinition Height='20' />
					<RowDefinition Height='20' />
					<RowDefinition Height='Auto' />
				</Grid.RowDefinitions>
				<StackPanel Orientation='Horizontal'>
					<TextBlock Text='Test' />
				</StackPanel>
				<local:TimeLineControl x:Name='t1' Grid.Row='1' />
				<TextBlock Grid.Row='2' Text='Test' />
			</Grid>
</UserControl>";
		
		int offset = @"<UserControl x:Class='ICSharpCode.Profiler.Controls.TimeLineCell'
	xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
	xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
".Length;
		
		string original = @"	<Grid>
		
	</Grid>";
		
		string replacement = @"			<Grid>
				<Grid.RowDefinitions>
					<RowDefinition Height='20' />
					<RowDefinition Height='20' />
					<RowDefinition Height='Auto' />
				</Grid.RowDefinitions>
				<StackPanel Orientation='Horizontal'>
					<TextBlock Text='Test' />
				</StackPanel>
				<local:TimeLineControl x:Name='t1' Grid.Row='1' />
				<TextBlock Grid.Row='2' Text='Test' />
			</Grid>";
		#endregion
		
		[Test]
		public void ReplacementTest1()
		{
			/*
			 * REPRODUCTION STEPS
			 * 
			 * 1. Run XmlDOM project
			 * 2. paste text from initialDocumentText (see Test Data region)
			 * 3. select lines 4 to 6
			 * 4. replace with replacement (see Test Data region)
			 * 5. exception thrown:
			 *    ICSharpCode.AvalonEdit.Xml.InternalException : Assertion failed: cached elements must not have zero length
			 *    at ICSharpCode.AvalonEdit.Xml.AXmlParser.Assert(Boolean condition, String message)
			 *  in c:\Projects\SharpDevelop\4.0\SharpDevelop\src\Libraries\AvalonEdit\ICSharpCode.AvalonEdit\Xml\AXmlParser.cs:line 121
			 *    at ICSharpCode.AvalonEdit.Xml.TagReader.TryReadFromCacheOrNew[T](T& res, Predicate`1 condition)
			 *  in c:\Projects\SharpDevelop\4.0\SharpDevelop\src\Libraries\AvalonEdit\ICSharpCode.AvalonEdit\Xml\TagReader.cs:line 39
			 *    at ICSharpCode.AvalonEdit.Xml.TagReader.<ReadText>d__12.MoveNext()
			 *  in c:\Projects\SharpDevelop\4.0\SharpDevelop\src\Libraries\AvalonEdit\ICSharpCode.AvalonEdit\Xml\TagReader.cs:line 456
			 *    at System.Collections.Generic.List`1.InsertRange(Int32 index, IEnumerable`1 collection)
			 *    at System.Collections.Generic.List`1.AddRange(IEnumerable`1 collection)
			 *    at ICSharpCode.AvalonEdit.Xml.TagReader.ReadAllTags()
			 *  in c:\Projects\SharpDevelop\4.0\SharpDevelop\src\Libraries\AvalonEdit\ICSharpCode.AvalonEdit\Xml\TagReader.cs:line 73
			 *    at ICSharpCode.AvalonEdit.Xml.AXmlParser.Parse(String input, IEnumerable`1 changesSinceLastParse)
			 *  in c:\Projects\SharpDevelop\4.0\SharpDevelop\src\Libraries\AvalonEdit\ICSharpCode.AvalonEdit\Xml\AXmlParser.cs:line 161
			 *    at ICSharpCode.AvalonEdit.Tests.XmlParser.TextReplacementTests.RunTest()
			 *  in c:\Projects\SharpDevelop\4.0\SharpDevelop\src\Libraries\AvalonEdit\ICSharpCode.AvalonEdit.Tests\XmlParser\TextReplacementTests.cs:line 114
			 *    at ICSharpCode.AvalonEdit.Tests.XmlParser.TextReplacementTests.TestMethod(
			 * ) in c:\Projects\SharpDevelop\4.0\SharpDevelop\src\Libraries\AvalonEdit\ICSharpCode.AvalonEdit.Tests\XmlParser\TextReplacementTests.cs:line 97
			 * */
			Assert.DoesNotThrow(RunTest1);
		}
		
		void RunTest1()
		{
			AXmlParser parser = new AXmlParser();
			
			try {
				parser.Lock.EnterWriteLock();
				
				parser.Parse(initialDocumentText, null); // full reparse
				
				IList<DocumentChangeEventArgs> changes = new List<DocumentChangeEventArgs>();
				
				changes.Add(new DocumentChangeEventArgs(offset, original, replacement));
				
				parser.Parse(finalDocumentText, changes);
			} finally {
				parser.Lock.ExitWriteLock();
			}
		}
	}
}
