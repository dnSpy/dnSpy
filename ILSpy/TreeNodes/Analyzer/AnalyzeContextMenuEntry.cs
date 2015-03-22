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
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	[ExportContextMenuEntryAttribute(Header = "Analyze", Icon = "images/Search.png")]
	internal sealed class AnalyzeContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			if (context.TreeView is AnalyzerTreeView && context.SelectedTreeNodes != null && context.SelectedTreeNodes.All(n => n.Parent.IsRoot))
				return false;
			if (context.SelectedTreeNodes == null)
				return context.Reference != null && context.Reference.Reference is MemberReference;
			return context.SelectedTreeNodes.All(n => n is IMemberTreeNode);
		}

		public bool IsEnabled(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return context.Reference != null && context.Reference.Reference is MemberReference;
			foreach (IMemberTreeNode node in context.SelectedTreeNodes) {
				if (!(node.Member is TypeDefinition
				      || node.Member is FieldDefinition
				      || node.Member is MethodDefinition
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
			} else if (context.Reference != null && context.Reference.Reference is MemberReference) {
				if (context.Reference.Reference is MemberReference)
					Analyze((MemberReference)context.Reference.Reference);
				// TODO: implement support for other references: ParameterReference, etc.
			}
		}

		public static void Analyze(MemberReference member)
		{
			if (member is TypeReference) {
				TypeDefinition type = ((TypeReference)member).Resolve();
				if (type != null)
					AnalyzerTreeView.Instance.ShowOrFocus(new AnalyzedTypeTreeNode(type));
			}
			else if (member is FieldReference) {
				FieldDefinition field = ((FieldReference)member).Resolve();
				if (field != null)
					AnalyzerTreeView.Instance.ShowOrFocus(new AnalyzedFieldTreeNode(field));
			}
			else if (member is MethodReference) {
				MethodDefinition method = ((MethodReference)member).Resolve();
				if (method != null)
					AnalyzerTreeView.Instance.ShowOrFocus(new AnalyzedMethodTreeNode(method));
			}
			else if (member is PropertyReference) {
				PropertyDefinition property = ((PropertyReference)member).Resolve();
				if (property != null)
					AnalyzerTreeView.Instance.ShowOrFocus(new AnalyzedPropertyTreeNode(property));
			}
			else if (member is EventReference) {
				EventDefinition @event = ((EventReference)member).Resolve();
				if (@event != null)
					AnalyzerTreeView.Instance.ShowOrFocus(new AnalyzedEventTreeNode(@event));
			}
		}
	}
}
