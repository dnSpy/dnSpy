using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.NRefactory.CSharp;
using System.IO;
using ICSharpCode.NRefactory.Editor;
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.FormattingTests
{
	public abstract class TestBase
	{
		static IActionFactory factory = new TestFactory ();
		
		class TestTextReplaceAction : TextReplaceAction
		{
			public TestTextReplaceAction (int offset, int removedChars, string insertedText) : base (offset, removedChars, insertedText)
			{
			}
			
			public override void Perform (Script script)
			{
			}
		}
		
		class TestFactory : AbstractActionFactory
		{
			public override TextReplaceAction CreateTextReplaceAction (int offset, int removedChars, string insertedText)
			{
				return new TestTextReplaceAction (offset, removedChars, insertedText);
			}
		}
		
		static string ApplyChanges (string text, List<TextReplaceAction> changes)
		{
			changes.Sort ((x, y) => y.Offset.CompareTo (x.Offset));
			StringBuilder b = new StringBuilder(text);
			foreach (var change in changes) {
//				Console.WriteLine ("---- apply:" + change);
//				Console.WriteLine (adapter.Text);
				if (change.Offset > b.Length)
					continue;
				b.Remove(change.Offset, change.RemovedChars);
				b.Insert(change.Offset, change.InsertedText);
			}
//			Console.WriteLine ("---result:");
//			Console.WriteLine (adapter.Text);
			return b.ToString();
		}
		
		protected static IDocument GetResult (CSharpFormattingOptions policy, string input)
		{
			var adapter = new ReadOnlyDocument (input);
			var visitor = new AstFormattingVisitor (policy, adapter, factory);
			
			var compilationUnit = new CSharpParser ().Parse (new StringReader (input));
			compilationUnit.AcceptVisitor (visitor, null);
			
			return new ReadOnlyDocument(ApplyChanges (input, visitor.Changes));
		}
		
		protected static IDocument Test (CSharpFormattingOptions policy, string input, string expectedOutput)
		{
			IDocument doc = GetResult(policy, input);
			if (expectedOutput != doc.Text) {
				Console.WriteLine (doc.Text);
			}
			Assert.AreEqual (expectedOutput, doc.Text);
			return doc;
		}

		protected static void Continue (CSharpFormattingOptions policy, IDocument document, string expectedOutput)
		{
			var visitior = new AstFormattingVisitor (policy, document, factory);
			
			var compilationUnit = new CSharpParser ().Parse (new StringReader (document.Text));
			compilationUnit.AcceptVisitor (visitior, null);
			string newText = ApplyChanges (document.Text, visitior.Changes);
			if (expectedOutput != newText) {
				Console.WriteLine (newText);
			}
			Assert.AreEqual (expectedOutput, newText);
		}

		
	}
}

