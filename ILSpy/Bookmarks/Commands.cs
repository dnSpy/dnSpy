// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using ICSharpCode.ILSpy.TreeNodes.Analyzer;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.Bookmarks
{
	[ExportBookmarkContextMenuEntry(Header = "Analyze", Icon = "images/Search.png", Category="Default")]
	internal sealed class AnalyzeBookmarkEntry : IBookmarkContextMenuEntry
	{
		public bool IsVisible(IBookmark[] marks)
		{
			return true;
		}

		public bool IsEnabled(IBookmark[] marks)
		{
			return true;
		}

		public void Execute(IBookmark[] marks)
		{
			foreach (var node in marks) {
				if (!(node is MemberBookmark))
					continue;
				
				var member = (node as MemberBookmark).Node.Annotation<MemberReference>();
				TypeDefinition type = member as TypeDefinition;
				if (type != null)
					AnalyzerTreeView.Instance.Show(new AnalyzedTypeTreeNode(type));
				FieldDefinition field = member as FieldDefinition;
				if (field != null)
					AnalyzerTreeView.Instance.Show(new AnalyzedFieldTreeNode(field));
				MethodDefinition method = member as MethodDefinition;
				if (method != null)
					AnalyzerTreeView.Instance.Show(new AnalyzedMethodTreeNode(method));
				var propertyAnalyzer = AnalyzedPropertyTreeNode.TryCreateAnalyzer(member);
				if (propertyAnalyzer != null)
					AnalyzerTreeView.Instance.Show(propertyAnalyzer);
				var eventAnalyzer = AnalyzedEventTreeNode.TryCreateAnalyzer(member);
				if (eventAnalyzer != null)
					AnalyzerTreeView.Instance.Show(eventAnalyzer);
			}
		}
	}
}
