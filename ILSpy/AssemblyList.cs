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
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using System.Xml.Linq;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// A list of assemblies.
	/// </summary>
	class AssemblyList
	{
		public AssemblyList(string listName)
		{
			this.ListName = listName;
			Assemblies.CollectionChanged += Assemblies_CollectionChanged;
		}
		
		public AssemblyList(XElement listElement)
			: this((string)listElement.Attribute("name"))
		{
			foreach (var asm in listElement.Elements("Assembly")) {
				OpenAssembly((string)asm);
			}
			this.Dirty = false; // OpenAssembly() sets dirty, so reset it since we just loaded...
		}
		
		public XElement Save()
		{
			return new XElement(
				"List",
				new XAttribute("name", this.ListName),
				Assemblies.Select(asm => new XElement("Assembly", asm.FileName))
			);
		}
		
		public bool Dirty { get; set; }
		public string ListName { get; set; }
		
		public readonly ObservableCollection<AssemblyTreeNode> Assemblies = new ObservableCollection<AssemblyTreeNode>();
		
		ConcurrentDictionary<TypeDefinition, TypeTreeNode> typeDict = new ConcurrentDictionary<TypeDefinition, TypeTreeNode>();

		void Assemblies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			this.Dirty = true;
			App.Current.Dispatcher.BeginInvoke(
				DispatcherPriority.Normal,
				new Action(
					delegate {
						if (this.Dirty) {
							this.Dirty = false;
							AssemblyListManager.SaveList(this);
						}
					})
			);
		}
		
		public void RegisterTypeNode(TypeTreeNode node)
		{
			// called on background loading thread, so we need to use a ConcurrentDictionary
			typeDict[node.TypeDefinition] = node;
		}
		
		public TypeTreeNode FindTypeNode(TypeDefinition def)
		{
			if (def == null)
				return null;
			if (def.DeclaringType != null) {
				TypeTreeNode decl = FindTypeNode(def.DeclaringType);
				if (decl != null) {
					decl.EnsureLazyChildren();
					return decl.VisibleChildren.OfType<TypeTreeNode>().FirstOrDefault(t => t.TypeDefinition == def);
				}
			} else {
				TypeTreeNode node;
				if (typeDict.TryGetValue(def, out node)) {
					// Ensure that the node is connected to the tree
					node.ParentAssemblyNode.EnsureLazyChildren();
					// Validate that the node wasn't removed due to visibility settings:
					if (node.Ancestors().OfType<AssemblyListTreeNode>().Any(n => n.AssemblyList == this))
						return node;
				}
			}
			return null;
		}
		
		public MethodTreeNode FindMethodNode(MethodDefinition def)
		{
			if (def == null)
				return null;
			TypeTreeNode typeNode = FindTypeNode(def.DeclaringType);
			typeNode.EnsureLazyChildren();
			MethodTreeNode methodNode = typeNode.VisibleChildren.OfType<MethodTreeNode>().FirstOrDefault(m => m.MethodDefinition == def);
			if (methodNode != null)
				return methodNode;
			foreach (var p in typeNode.VisibleChildren.OfType<ILSpyTreeNode<MethodTreeNode>>()) {
				// method might be a child or a property or events
				p.EnsureLazyChildren();
				methodNode = p.Children.FirstOrDefault(m => m.MethodDefinition == def);
				if (methodNode != null)
					return methodNode;
			}
			
			return null;
		}
		
		public FieldTreeNode FindFieldNode(FieldDefinition def)
		{
			if (def == null)
				return null;
			TypeTreeNode typeNode = FindTypeNode(def.DeclaringType);
			typeNode.EnsureLazyChildren();
			return typeNode.VisibleChildren.OfType<FieldTreeNode>().FirstOrDefault(m => m.FieldDefinition == def);
		}
		
		public PropertyTreeNode FindPropertyNode(PropertyDefinition def)
		{
			if (def == null)
				return null;
			TypeTreeNode typeNode = FindTypeNode(def.DeclaringType);
			typeNode.EnsureLazyChildren();
			return typeNode.VisibleChildren.OfType<PropertyTreeNode>().FirstOrDefault(m => m.PropertyDefinition == def);
		}
		
		public EventTreeNode FindEventNode(EventDefinition def)
		{
			if (def == null)
				return null;
			TypeTreeNode typeNode = FindTypeNode(def.DeclaringType);
			typeNode.EnsureLazyChildren();
			return typeNode.VisibleChildren.OfType<EventTreeNode>().FirstOrDefault(m => m.EventDefinition == def);
		}
		
		public AssemblyTreeNode OpenAssembly(string file)
		{
			App.Current.Dispatcher.VerifyAccess();
			
			file = Path.GetFullPath(file);
			
			foreach (AssemblyTreeNode node in this.Assemblies) {
				if (file.Equals(node.FileName, StringComparison.OrdinalIgnoreCase))
					return node;
			}
			
			var newNode = new AssemblyTreeNode(file, this);
			this.Assemblies.Add(newNode);
			return newNode;
		}
	}
}
