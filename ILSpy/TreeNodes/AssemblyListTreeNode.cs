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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Represents a list of assemblies.
	/// This is used as (invisible) root node of the tree view.
	/// </summary>
	sealed class AssemblyListTreeNode : ILSpyTreeNode
	{
		readonly AssemblyList assemblyList;

		public AssemblyList AssemblyList
		{
			get { return assemblyList; }
		}

		public AssemblyListTreeNode(AssemblyList assemblyList)
		{
			if (assemblyList == null)
				throw new ArgumentNullException("assemblyList");
			this.assemblyList = assemblyList;
			BindToObservableCollection(assemblyList.assemblies);
		}

		public override object Text
		{
			get { return assemblyList.ListName; }
		}

		void BindToObservableCollection(ObservableCollection<LoadedAssembly> collection)
		{
			this.Children.Clear();
			this.Children.AddRange(collection.Select(a => new AssemblyTreeNode(a)));
			collection.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e) {
				switch (e.Action) {
					case NotifyCollectionChangedAction.Add:
						this.Children.InsertRange(e.NewStartingIndex, e.NewItems.Cast<LoadedAssembly>().Select(a => new AssemblyTreeNode(a)));
						break;
					case NotifyCollectionChangedAction.Remove:
						this.Children.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
						break;
					case NotifyCollectionChangedAction.Replace:
					case NotifyCollectionChangedAction.Move:
						throw new NotImplementedException();
					case NotifyCollectionChangedAction.Reset:
						this.Children.Clear();
						this.Children.AddRange(collection.Select(a => new AssemblyTreeNode(a)));
						break;
					default:
						throw new NotSupportedException("Invalid value for NotifyCollectionChangedAction");
				}
			};
		}

		public override bool CanDrop(DragEventArgs e, int index)
		{
			e.Effects = DragDropEffects.Move;
			if (e.Data.GetDataPresent(AssemblyTreeNode.DataFormat))
				return true;
			else if (e.Data.GetDataPresent(DataFormats.FileDrop))
				return true;
			else {
				e.Effects = DragDropEffects.None;
				return false;
			}
		}

		public override void Drop(DragEventArgs e, int index)
		{
			string[] files = e.Data.GetData(AssemblyTreeNode.DataFormat) as string[];
			if (files == null)
				files = e.Data.GetData(DataFormats.FileDrop) as string[];
			if (files != null) {
				lock (assemblyList.assemblies) {
					var assemblies = (from file in files
									  where file != null
									  select assemblyList.OpenAssembly(file) into node
									  where node != null
									  select node).Distinct().ToList();
					foreach (LoadedAssembly asm in assemblies) {
						int nodeIndex = assemblyList.assemblies.IndexOf(asm);
						if (nodeIndex < index)
							index--;
						assemblyList.assemblies.RemoveAt(nodeIndex);
					}
					assemblies.Reverse();
					foreach (LoadedAssembly asm in assemblies) {
						assemblyList.assemblies.Insert(index, asm);
					}
				}
			}
		}

		public Action<SharpTreeNode> Select = delegate { };

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.WriteCommentLine(output, "List: " + assemblyList.ListName);
			output.WriteLine();
			foreach (AssemblyTreeNode asm in this.Children) {
				language.WriteCommentLine(output, new string('-', 60));
				output.WriteLine();
				asm.Decompile(language, output, options);
			}
		}

		#region Find*Node

		public AssemblyTreeNode FindAssemblyNode(AssemblyDefinition asm)
		{
			if (asm == null)
				return null;
			App.Current.Dispatcher.VerifyAccess();
			foreach (AssemblyTreeNode node in this.Children) {
				if (node.LoadedAssembly.IsLoaded && node.LoadedAssembly.AssemblyDefinition == asm)
					return node;
			}
			return null;
		}

		public AssemblyTreeNode FindAssemblyNode(LoadedAssembly asm)
		{
			if (asm == null)
				return null;
			App.Current.Dispatcher.VerifyAccess();
			foreach (AssemblyTreeNode node in this.Children) {
				if (node.LoadedAssembly == asm)
					return node;
			}
			return null;
		}

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
					return decl.Children.OfType<TypeTreeNode>().FirstOrDefault(t => t.TypeDefinition == def && !t.IsHidden);
				}
			} else {
				AssemblyTreeNode asm = FindAssemblyNode(def.Module.Assembly);
				if (asm != null) {
					return asm.FindTypeNode(def);
				}
			}
			return null;
		}

		/// <summary>
		/// Looks up the method node corresponding to the method definition.
		/// Returns null if no matching node is found.
		/// </summary>
		public SharpTreeNode FindMethodNode(MethodDefinition def)
		{
			if (def == null)
				return null;
			TypeTreeNode typeNode = FindTypeNode(def.DeclaringType);
			if (typeNode == null)
				return null;
			typeNode.EnsureLazyChildren();
			MethodTreeNode methodNode = typeNode.Children.OfType<MethodTreeNode>().FirstOrDefault(m => m.MethodDefinition == def && !m.IsHidden);
			if (methodNode != null)
				return methodNode;
			foreach (var p in typeNode.Children.OfType<ILSpyTreeNode>()) {
				if (p.IsHidden)
					continue;

				// method might be a child of a property or event
				if (p is PropertyTreeNode || p is EventTreeNode) {
					p.EnsureLazyChildren();
					methodNode = p.Children.OfType<MethodTreeNode>().FirstOrDefault(m => m.MethodDefinition == def);
					if (methodNode != null) {
						/// If the requested method is a property or event accessor, and accessors are
						/// hidden in the UI, then return the owning property or event.
						if (methodNode.IsHidden)
							return p;
						else
							return methodNode;
					}
				}
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
			if (typeNode == null)
				return null;
			typeNode.EnsureLazyChildren();
			return typeNode.Children.OfType<FieldTreeNode>().FirstOrDefault(m => m.FieldDefinition == def && !m.IsHidden);
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
			if (typeNode == null)
				return null;
			typeNode.EnsureLazyChildren();
			return typeNode.Children.OfType<PropertyTreeNode>().FirstOrDefault(m => m.PropertyDefinition == def && !m.IsHidden);
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
			if (typeNode == null)
				return null;
			typeNode.EnsureLazyChildren();
			return typeNode.Children.OfType<EventTreeNode>().FirstOrDefault(m => m.EventDefinition == def && !m.IsHidden);
		}
		#endregion
	}
}
