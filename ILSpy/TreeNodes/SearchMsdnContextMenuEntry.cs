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
using System.Diagnostics;
using Mono.Cecil;
using ICSharpCode.TreeView;
using ICSharpCode.ILSpy.TreeNodes.Analyzer;

namespace ICSharpCode.ILSpy.TreeNodes
{
	[ExportContextMenuEntryAttribute(Header = "Search MSDN...", Icon = "images/Search.png")]
	internal sealed class SearchMsdnContextMenuEntry : IContextMenuEntry
	{
		private static string msdnAddress = "http://msdn.microsoft.com/en-us/library/{0}";

		public bool IsVisible(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;

			return context.SelectedTreeNodes.All(
				n => n is NamespaceTreeNode
				|| n is TypeTreeNode
				|| n is EventTreeNode
				|| n is FieldTreeNode
				|| n is PropertyTreeNode
				|| n is MethodTreeNode);
		}

		public bool IsEnabled(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;

			foreach (var node in context.SelectedTreeNodes)
			{
				var typeNode = node as TypeTreeNode;
				if (typeNode != null && !typeNode.IsPublicAPI)
					return false;

				var eventNode = node as EventTreeNode;
				if (eventNode != null && (!eventNode.IsPublicAPI || !eventNode.EventDefinition.DeclaringType.IsPublic))
					return false;

				var fieldNode = node as FieldTreeNode;
				if (fieldNode != null && (!fieldNode.IsPublicAPI || !fieldNode.FieldDefinition.DeclaringType.IsPublic))
					return false;

				var propertyNode = node as PropertyTreeNode;
				if (propertyNode != null && (!propertyNode.IsPublicAPI || !propertyNode.PropertyDefinition.DeclaringType.IsPublic))
					return false;

				var methodNode = node as MethodTreeNode;
				if (methodNode != null && (!methodNode.IsPublicAPI || !methodNode.MethodDefinition.DeclaringType.IsPublic))
					return false;

				var namespaceNode = node as NamespaceTreeNode;
				if (namespaceNode != null && string.IsNullOrEmpty(namespaceNode.Name))
					return false;
			}

			return true;
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes != null) {
				foreach (ILSpyTreeNode node in context.SelectedTreeNodes) {
					SearchMsdn(node);
				}
			}
		}

		public static void SearchMsdn(ILSpyTreeNode node)
		{
			var address = string.Empty;

			var namespaceNode = node as NamespaceTreeNode;
			if (namespaceNode != null)
				address = string.Format(msdnAddress, namespaceNode.Name);

			var memberNode = node as IMemberTreeNode;
			if (memberNode != null)
			{
				var member = memberNode.Member;
				var memberName = string.Empty;

				if (member.DeclaringType == null)
					memberName = member.FullName;
				else
					memberName = string.Format("{0}.{1}", member.DeclaringType.FullName, member.Name);

				address = string.Format(msdnAddress, memberName);
			}

			address = address.ToLower();
			if (!string.IsNullOrEmpty(address))
				Process.Start(address);
		}
	}
}