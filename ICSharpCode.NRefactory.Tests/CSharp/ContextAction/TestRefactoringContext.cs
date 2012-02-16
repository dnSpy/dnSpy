// 
// TestRefactoringContext.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.FormattingTests;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.ContextActions
{
	class TestRefactoringContext : RefactoringContext
	{
		internal IDocument doc;
		CSharpParsedFile parsedFile;
		CSharpAstResolver resolver;
		
		public override bool HasCSharp3Support {
			get {
				return true;
			}
		}
		

		public override CSharpFormattingOptions FormattingOptions {
			get {
				return new CSharpFormattingOptions ();
			}
		}
		
		public override AstType CreateShortType (IType fullType)
		{
			AstNode node = Unit.GetNodeAt(Location);
			CSharpResolver csResolver = resolver.GetResolverStateBefore(node);
			var builder = new TypeSystemAstBuilder (csResolver);
			return builder.ConvertType (fullType);
		}
		
		public override void ReplaceReferences (IMember member, MemberDeclaration replaceWidth)
		{
//			throw new NotImplementedException ();
		}
		
		class MyScript : Script
		{
			TestRefactoringContext trc;
			
			public MyScript (TestRefactoringContext trc) : base (trc)
			{
				this.trc = trc;
			}
			
			public override void Dispose ()
			{
				trc.doc = new ReadOnlyDocument (TestBase.ApplyChanges (trc.doc.Text, new List<TextReplaceAction> (Actions.Where (act => act is TextReplaceAction).Cast<TextReplaceAction>())));
			}
			
			public override void InsertWithCursor (string operation, AstNode node, InsertPosition defaultPosition)
			{
				throw new NotImplementedException ();
			}
		}
		public override Script StartScript ()
		{
			return new MyScript (this);
		}
		
		#region Text stuff
		public override string EolMarker { get { return Environment.NewLine; } }

		public override bool IsSomethingSelected { get { return SelectionStart > 0; }  }

		public override string SelectedText { get { return IsSomethingSelected ? doc.GetText (SelectionStart, SelectionLength) : ""; } }
		
		int selectionStart;
		public override int SelectionStart { get { return selectionStart; } }
		
		int selectionEnd;
		public override int SelectionEnd { get { return selectionEnd; } }

		public override int SelectionLength { get { return IsSomethingSelected ? SelectionEnd - SelectionStart : 0; } }

		public override int GetOffset (TextLocation location)
		{
			return doc.GetOffset (location);
		}
		
		public override TextLocation GetLocation (int offset)
		{
			return doc.GetLocation (offset);
		}

		public override string GetText (int offset, int length)
		{
			return doc.GetText (offset, length);
		}
		#endregion
		
		#region Resolving
		public override ResolveResult Resolve (AstNode node)
		{
			return resolver.Resolve(node);
		}		
		#endregion
		
		
		
		public TestRefactoringContext (string content)
		{
			int idx = content.IndexOf ("$");
			if (idx >= 0)
				content = content.Substring (0, idx) + content.Substring (idx + 1);
			int idx1 = content.IndexOf ("<-");
			int idx2 = content.IndexOf ("->");
			
			if (0 <= idx1 && idx1 < idx2) {
				content = content.Substring (0, idx2) + content.Substring (idx2 + 2);
				content = content.Substring (0, idx1) + content.Substring (idx1 + 2);
				selectionStart = idx1;
				idx = selectionEnd = idx2 - 2;
			}
			
			doc = new ReadOnlyDocument (content);
			var parser = new CSharpParser ();
			Unit = parser.Parse (content, "program.cs");
			if (parser.HasErrors)
				parser.ErrorPrinter.Errors.ForEach (e => Console.WriteLine (e.Message));
			Assert.IsFalse (parser.HasErrors, "File contains parsing errors.");
			parsedFile = Unit.ToTypeSystem();
			
			IProjectContent pc = new CSharpProjectContent();
			pc = pc.UpdateProjectContent(null, parsedFile);
			pc = pc.AddAssemblyReferences(new[] { CecilLoaderTests.Mscorlib, CecilLoaderTests.SystemCore });
			
			Compilation = pc.CreateCompilation();
			resolver = new CSharpAstResolver(Compilation, Unit, parsedFile);
			if (idx >= 0)
				Location = doc.GetLocation (idx);
		}
		
		internal static void Print (AstNode node)
		{
			var v = new CSharpOutputVisitor (Console.Out, new CSharpFormattingOptions ());
			node.AcceptVisitor (v, null);
		}
		
		#region IActionFactory implementation
		public override TextReplaceAction CreateTextReplaceAction (int offset, int removedChars, string insertedText)
		{
			return new TestBase.TestTextReplaceAction (offset, removedChars, insertedText);
		}

		public override NodeOutputAction CreateNodeOutputAction (int offset, int removedChars, NodeOutput output)
		{
			return new TestNodeOutputAction (offset, removedChars, output);
		}

		public override NodeSelectionAction CreateNodeSelectionAction (AstNode node)
		{
			return new TestNodeSelectAction (node);
		}

		public override FormatTextAction CreateFormatTextAction (Func<RefactoringContext, AstNode> callback)
		{
			throw new NotImplementedException ();
		}

		public override CreateLinkAction CreateLinkAction (IEnumerable<AstNode> linkedNodes)
		{
			return new TestNodeLinkAction (linkedNodes);
		}
		
		class TestNodeLinkAction : CreateLinkAction
		{
			public TestNodeLinkAction (IEnumerable<AstNode> linkedNodes) : base (linkedNodes)
			{
			}
			
			public override void Perform (Script script)
			{
			}
		}
		class TestNodeSelectAction : NodeSelectionAction
		{
			public TestNodeSelectAction (AstNode astNode) : base (astNode)
			{
			}
			public override void Perform (Script script)
			{
			}
		}
		
		public class TestNodeOutputAction : NodeOutputAction
		{
			public TestNodeOutputAction (int offset, int removedChars, NodeOutput output) : base (offset, removedChars, output)
			{
			}
			
			public override void Perform (Script script)
			{
			}
		}
		#endregion
	}
	
}
