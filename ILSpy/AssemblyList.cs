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
	sealed class AssemblyList
	{
		readonly string listName;
		
		/// <summary>Dirty flag, used to mark modifications so that the list is saved later</summary>
		bool dirty;
		
		/// <summary>
		/// The assemblies in this list.
		/// Needs locking for multi-threaded access!
		/// Write accesses are allowed on the GUI thread only (but still need locking!)
		/// </summary>
		internal readonly ObservableCollection<AssemblyTreeNode> assemblies = new ObservableCollection<AssemblyTreeNode>();
		
		/// <summary>
		/// Dictionary for quickly finding types (used in hyperlink navigation)
		/// </summary>
		readonly ConcurrentDictionary<TypeDefinition, TypeTreeNode> typeDict = new ConcurrentDictionary<TypeDefinition, TypeTreeNode>();
		
		public AssemblyList(string listName)
		{
			this.listName = listName;
			assemblies.CollectionChanged += Assemblies_CollectionChanged;
		}
		
		/// <summary>
		/// Loads an assembly list from XML.
		/// </summary>
		public AssemblyList(XElement listElement)
			: this((string)listElement.Attribute("name"))
		{
			foreach (var asm in listElement.Elements("Assembly")) {
				OpenAssembly((string)asm);
			}
			this.dirty = false; // OpenAssembly() sets dirty, so reset it afterwards
		}
		
		/// <summary>
		/// Gets the loaded assemblies. This method is thread-safe.
		/// </summary>
		public AssemblyTreeNode[] GetAssemblies()
		{
			lock (assemblies) {
				return assemblies.ToArray();
			}
		}
		
		/// <summary>
		/// Saves this assembly list to XML.
		/// </summary>
		public XElement SaveAsXml()
		{
			return new XElement(
				"List",
				new XAttribute("name", this.ListName),
				assemblies.Select(asm => new XElement("Assembly", asm.FileName))
			);
		}
		
		/// <summary>
		/// Gets the name of this list.
		/// </summary>
		public string ListName {
			get { return listName; }
		}
		
		void Assemblies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// Whenever the assembly list is modified, mark it as dirty
			// and enqueue a task that saves it once the UI has finished modifying the assembly list.
			if (!dirty) {
				dirty = true;
				App.Current.Dispatcher.BeginInvoke(
					DispatcherPriority.Background,
					new Action(
						delegate {
							dirty = false;
							AssemblyListManager.SaveList(this);
						})
				);
			}
		}
		
		/// <summary>
		/// Registers a type node in the dictionary for quick type lookup.
		/// </summary>
		/// <remarks>This method is called by the assembly loading code (on a background thread)</remarks>
		public void RegisterTypeNode(TypeTreeNode node)
		{
			// called on background loading thread, so we need to use a ConcurrentDictionary
			typeDict[node.TypeDefinition] = node;
		}
		
		#region Find*Node
		/// <summary>
		/// Looks up the type node corresponding to the type definition.
		/// Returns null if no matching node is found.
		/// </summary>
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
		
		/// <summary>
		/// Looks up the method node corresponding to the method definition.
		/// Returns null if no matching node is found.
		/// </summary>
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
		
		/// <summary>
		/// Looks up the field node corresponding to the field definition.
		/// Returns null if no matching node is found.
		/// </summary>
		public FieldTreeNode FindFieldNode(FieldDefinition def)
		{
			if (def == null)
				return null;
			TypeTreeNode typeNode = FindTypeNode(def.DeclaringType);
			typeNode.EnsureLazyChildren();
			return typeNode.VisibleChildren.OfType<FieldTreeNode>().FirstOrDefault(m => m.FieldDefinition == def);
		}
		
		/// <summary>
		/// Looks up the property node corresponding to the property definition.
		/// Returns null if no matching node is found.
		/// </summary>
		public PropertyTreeNode FindPropertyNode(PropertyDefinition def)
		{
			if (def == null)
				return null;
			TypeTreeNode typeNode = FindTypeNode(def.DeclaringType);
			typeNode.EnsureLazyChildren();
			return typeNode.VisibleChildren.OfType<PropertyTreeNode>().FirstOrDefault(m => m.PropertyDefinition == def);
		}
		
		/// <summary>
		/// Looks up the event node corresponding to the event definition.
		/// Returns null if no matching node is found.
		/// </summary>
		public EventTreeNode FindEventNode(EventDefinition def)
		{
			if (def == null)
				return null;
			TypeTreeNode typeNode = FindTypeNode(def.DeclaringType);
			typeNode.EnsureLazyChildren();
			return typeNode.VisibleChildren.OfType<EventTreeNode>().FirstOrDefault(m => m.EventDefinition == def);
		}
		#endregion
		
		/// <summary>
		/// Opens an assembly from disk.
		/// Returns the existing assembly node if it is already loaded.
		/// </summary>
		public AssemblyTreeNode OpenAssembly(string file)
		{
			App.Current.Dispatcher.VerifyAccess();
			
			file = Path.GetFullPath(file);
			
			foreach (AssemblyTreeNode node in this.assemblies) {
				if (file.Equals(node.FileName, StringComparison.OrdinalIgnoreCase))
					return node;
			}
			
			var newNode = new AssemblyTreeNode(file, this);
			this.assemblies.Add(newNode);
			return newNode;
		}
	}
}
