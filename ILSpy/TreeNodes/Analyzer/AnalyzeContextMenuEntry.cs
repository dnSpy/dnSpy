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
using System.Linq;
using System.Windows;
using ICSharpCode.TreeView;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	[ExportContextMenuEntryAttribute(Header = "_Analyze", Icon = "images/Search.png", Order = 900, Category = "Other")]
	internal sealed class AnalyzeContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			if (context.TreeView is AnalyzerTreeView && context.SelectedTreeNodes != null && context.SelectedTreeNodes.All(n => n.Parent.IsRoot))
				return false;
			if (context.SelectedTreeNodes == null)
				return context.Reference != null && context.Reference.Reference is IMemberRef;
			return context.SelectedTreeNodes.All(n => n is IMemberTreeNode);
		}

		public bool IsEnabled(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return context.Reference != null && context.Reference.Reference is IMemberRef;
			foreach (IMemberTreeNode node in context.SelectedTreeNodes) {
				if (!(node.Member is TypeDef
				      || node.Member is FieldDef
				      || node.Member is MethodDef
				      || AnalyzedPropertyTreeNode.CanShow(node.Member)
				      || AnalyzedEventTreeNode.CanShow(node.Member)))
					return false;
			}

			return true;
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes != null) {
				foreach (IMemberTreeNode node in context.SelectedTreeNodes) {
					Analyze(node.Member);
				}
			} else if (context.Reference != null && context.Reference.Reference is IMemberRef) {
				if (context.Reference.Reference is IMemberRef)
					Analyze((IMemberRef)context.Reference.Reference);
				// TODO: implement support for other references: ParameterReference, etc.
			}
		}

		public static void Analyze(IMemberRef member)
		{
			TypeDef type = null;
			if (member is ITypeDefOrRef)
				type = ((ITypeDefOrRef)member).ResolveTypeDef();
			if (type != null)
				AnalyzerTreeView.Instance.ShowOrFocus(new AnalyzedTypeTreeNode(type));
			FieldDef field = member as FieldDef;
			if (field == null && member is IField && ((IField)member).IsField)
				field = ((IField)member).ResolveFieldDef();
			if (field != null)
				AnalyzerTreeView.Instance.ShowOrFocus(new AnalyzedFieldTreeNode(field));
			MethodDef method = member as MethodDef;
			if (method == null && member is IMethod && ((IMethod)member).IsMethod)
				method = ((IMethod)member).ResolveMethodDef();
			if (method != null)
				AnalyzerTreeView.Instance.ShowOrFocus(new AnalyzedMethodTreeNode(method));
			var propertyAnalyzer = AnalyzedPropertyTreeNode.TryCreateAnalyzer(member);
			if (propertyAnalyzer != null)
				AnalyzerTreeView.Instance.ShowOrFocus(propertyAnalyzer);
			var eventAnalyzer = AnalyzedEventTreeNode.TryCreateAnalyzer(member);
			if (eventAnalyzer != null)
				AnalyzerTreeView.Instance.ShowOrFocus(eventAnalyzer);
		}
	}
}
