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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Represents a list of assemblies.
	/// This is used as (invisible) root node of the tree view.
	/// </summary>
	sealed class AssemblyListTreeNode : ILSpyTreeNode
	{
		readonly AssemblyList assemblyList;

		public AssemblyListTreeNode(AssemblyList assemblyList)
		{
			if (assemblyList == null)
				throw new ArgumentNullException("assemblyList");
			this.assemblyList = assemblyList;
			BindToObservableCollection();
		}

		public override object Text
		{
			get { return ToString(Language); }
		}

		public override string ToString(Language language)
		{
			return CleanUpName(assemblyList.ListName);
		}

		void BindToObservableCollection()
		{
			this.Children.Clear();
			this.Children.AddRange(assemblyList.GetAssemblies().Select(a => CreateAssemblyTreeNode(a)));
			assemblyList.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e) {
				switch (e.Action) {
					case NotifyCollectionChangedAction.Add:
					this.Children.InsertRange(e.NewStartingIndex, e.NewItems.Cast<LoadedAssembly>().Select(a => CreateAssemblyTreeNode(a)));
						break;
					case NotifyCollectionChangedAction.Remove:
						this.Children.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
						break;
					case NotifyCollectionChangedAction.Replace:
					case NotifyCollectionChangedAction.Move:
						throw new NotImplementedException();
					case NotifyCollectionChangedAction.Reset:
						this.Children.Clear();
						this.Children.AddRange(assemblyList.GetAssemblies().Select(a => CreateAssemblyTreeNode(a)));
						break;
					default:
						throw new NotSupportedException("Invalid value for NotifyCollectionChangedAction");
				}
			};
		}

		AssemblyTreeNode CreateAssemblyTreeNode(LoadedAssembly asm)
		{
			CachedAssemblyTreeNode cachedInfo;
			if (cachedAsmTreeNodes.TryGetValue(asm, out cachedInfo)) {
				var asmNode = cachedInfo.AssemblyTreeNode;
				Debug.Assert(asmNode.Parent == null);
				if (asmNode.Parent != null)
					throw new InvalidOperationException();
				return asmNode;
			}
			return new AssemblyTreeNode(asm);
		}

		sealed class CachedAssemblyTreeNode
		{
			public AssemblyTreeNode AssemblyTreeNode;
			public int Counter;

			public CachedAssemblyTreeNode(AssemblyTreeNode asmNode)
			{
				this.AssemblyTreeNode = asmNode;
			}
		}

		readonly Dictionary<LoadedAssembly, CachedAssemblyTreeNode> cachedAsmTreeNodes = new Dictionary<LoadedAssembly, CachedAssemblyTreeNode>();

		public void RegisterCached(LoadedAssembly asm, AssemblyTreeNode asmNode)
		{
			CachedAssemblyTreeNode cachedInfo;
			if (!cachedAsmTreeNodes.TryGetValue(asm, out cachedInfo))
				cachedAsmTreeNodes.Add(asm, cachedInfo = new CachedAssemblyTreeNode(asmNode));
			else {
				Debug.Assert(cachedInfo.AssemblyTreeNode == asmNode);
				if (cachedInfo.AssemblyTreeNode != asmNode)
					throw new InvalidOperationException();
			}
			cachedInfo.Counter++;
		}

		public void UnregisterCached(LoadedAssembly asm)
		{
			var cachedInfo = cachedAsmTreeNodes[asm];
			if (cachedInfo.Counter-- == 1)
				cachedAsmTreeNodes.Remove(asm);
		}

		internal bool DisableDrop { get; set; }

		public override bool CanDrop(DragEventArgs e, int index)
		{
			if (DisableDrop) {
				e.Effects = DragDropEffects.None;
				return false;
			}

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
			Debug.Assert(!DisableDrop);
			if (DisableDrop)
				return;
			string[] files = e.Data.GetData(AssemblyTreeNode.DataFormat) as string[];
			if (files == null)
				files = e.Data.GetData(DataFormats.FileDrop) as string[];
			if (files != null && files.Length > 0) {
				LoadedAssembly newSelectedAsm = null;
				bool newSelectedAsmExisted = false;
				var oldIgnoreSelChg = MainWindow.Instance.TreeView_SelectionChanged_ignore;
				try {
					lock (assemblyList.GetLockObj()) {
						int numFiles = assemblyList.Count_NoLock;
						var old = assemblyList.IsReArranging;
						try {
							MainWindow.Instance.TreeView_SelectionChanged_ignore = true;
							var assemblies = (from file in files
											  where file != null
											  select assemblyList.OpenAssembly(file) into node
											  where node != null
											  select node).Distinct().ToList();
							var oldAsm = new Dictionary<LoadedAssembly, bool>(assemblies.Count);
							foreach (LoadedAssembly asm in assemblies) {
								int nodeIndex = assemblyList.IndexOf_NoLock(asm);
								oldAsm[asm] = nodeIndex < numFiles;
								if (newSelectedAsm == null) {
									newSelectedAsm = asm;
									newSelectedAsmExisted = oldAsm[asm];
								}
								if (nodeIndex < index)
									index--;
								numFiles--;
								assemblyList.IsReArranging = oldAsm[asm];
								assemblyList.RemoveAt_NoLock(nodeIndex);
								assemblyList.IsReArranging = old;
							}
							assemblies.Reverse();
							foreach (LoadedAssembly asm in assemblies) {
								assemblyList.IsReArranging = oldAsm[asm];
								assemblyList.Insert_NoLock(index, asm);
								assemblyList.IsReArranging = old;
							}
						}
						finally {
							assemblyList.IsReArranging = old;
						}
					}
					if (newSelectedAsm != null) {
						if (!newSelectedAsmExisted)
							MainWindow.Instance.TreeView_SelectionChanged_ignore = oldIgnoreSelChg;
						var node = MainWindow.Instance.FindTreeNode(newSelectedAsm.AssemblyDefinition) ??
							MainWindow.Instance.FindTreeNode(newSelectedAsm.ModuleDefinition);
						if (node != null) {
							MainWindow.Instance.treeView.FocusNode(node);
							MainWindow.Instance.treeView.SelectedItem = node;
						}
					}
				}
				finally {
					MainWindow.Instance.TreeView_SelectionChanged_ignore = oldIgnoreSelChg;
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

		public AssemblyTreeNode FindModuleNode(ModuleDef module)
		{
			if (module == null)
				return null;
			App.Current.Dispatcher.VerifyAccess();
			foreach (AssemblyTreeNode node in this.Children) {
				if (!node.LoadedAssembly.IsLoaded)
					continue;
				if (node.IsNetModule) {
					if (node.LoadedAssembly.ModuleDefinition == module)
						return node;
				}
				else {
					node.EnsureChildrenFiltered();
					foreach (var asmNode in node.Children.OfType<AssemblyTreeNode>()) {
						if (asmNode.LoadedAssembly.IsLoaded && asmNode.LoadedAssembly.ModuleDefinition == module)
							return asmNode;
					}
				}
			}
			return null;
		}
		
		public AssemblyTreeNode FindAssemblyNode(AssemblyDef asm)
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
		public TypeTreeNode FindTypeNode(TypeDef def)
		{
			if (def == null)
				return null;
			if (def.DeclaringType != null) {
				TypeTreeNode decl = FindTypeNode(def.DeclaringType);
				if (decl != null) {
					decl.EnsureChildrenFiltered();
					return decl.Children.OfType<TypeTreeNode>().FirstOrDefault(t => t.TypeDefinition == def);
				}
			} else {
				AssemblyTreeNode asm = FindModuleNode(def.Module);
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
		public ILSpyTreeNode FindMethodNode(MethodDef def)
		{
			if (def == null)
				return null;
			TypeTreeNode typeNode = FindTypeNode(def.DeclaringType);
			if (typeNode == null)
				return null;
			typeNode.EnsureChildrenFiltered();
			MethodTreeNode methodNode = typeNode.Children.OfType<MethodTreeNode>().FirstOrDefault(m => m.MethodDefinition == def);
			if (methodNode != null)
				return methodNode;
			foreach (var p in typeNode.Children.OfType<ILSpyTreeNode>()) {
				if (p is PropertyTreeNode || p is EventTreeNode) {
					p.EnsureChildrenFiltered();
					methodNode = p.Children.OfType<MethodTreeNode>().FirstOrDefault(m => m.MethodDefinition == def);
					if (methodNode != null)
						return methodNode;
				}
			}

			return null;
		}

		/// <summary>
		/// Looks up the field node corresponding to the field definition.
		/// Returns null if no matching node is found.
		/// </summary>
		public FieldTreeNode FindFieldNode(FieldDef def)
		{
			if (def == null)
				return null;
			TypeTreeNode typeNode = FindTypeNode(def.DeclaringType);
			if (typeNode == null)
				return null;
			typeNode.EnsureChildrenFiltered();
			return typeNode.Children.OfType<FieldTreeNode>().FirstOrDefault(m => m.FieldDefinition == def);
		}

		/// <summary>
		/// Looks up the property node corresponding to the property definition.
		/// Returns null if no matching node is found.
		/// </summary>
		public PropertyTreeNode FindPropertyNode(PropertyDef def)
		{
			if (def == null)
				return null;
			TypeTreeNode typeNode = FindTypeNode(def.DeclaringType);
			if (typeNode == null)
				return null;
			typeNode.EnsureChildrenFiltered();
			return typeNode.Children.OfType<PropertyTreeNode>().FirstOrDefault(m => m.PropertyDefinition == def);
		}

		/// <summary>
		/// Looks up the event node corresponding to the event definition.
		/// Returns null if no matching node is found.
		/// </summary>
		public EventTreeNode FindEventNode(EventDef def)
		{
			if (def == null)
				return null;
			TypeTreeNode typeNode = FindTypeNode(def.DeclaringType);
			if (typeNode == null)
				return null;
			typeNode.EnsureChildrenFiltered();
			return typeNode.Children.OfType<EventTreeNode>().FirstOrDefault(m => m.EventDefinition == def);
		}
		#endregion

		public LoadedAssembly FindModule(LoadedAssembly asm, string moduleFilename)
		{
			App.Current.Dispatcher.VerifyAccess();
			foreach (AssemblyTreeNode node in this.Children) {
				if (node.LoadedAssembly != asm)
					continue;
				if (node.IsNetModule)
					continue;

				node.EnsureChildrenFiltered();
				foreach (var asmNode in node.Children.OfType<AssemblyTreeNode>()) {
					if (string.IsNullOrWhiteSpace(asmNode.LoadedAssembly.FileName))
						continue;
					if (asmNode.LoadedAssembly.FileName.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
						return asmNode.LoadedAssembly;
				}
			}

			return null;
		}

		/// <summary>
		/// Retrieves a node using the NodePathName property of its ancestors.
		/// </summary>
		public ILSpyTreeNode FindNodeByPath(FullNodePathName fullPath)
		{
			ILSpyTreeNode node = this;
			foreach (var name in fullPath.Names) {
				if (node == null)
					break;
				node.EnsureChildrenFiltered();
				node = (ILSpyTreeNode)node.Children.FirstOrDefault(c => ((ILSpyTreeNode)c).NodePathName == name);
			}
			return node == this ? null : node;
		}

		public ILSpyTreeNode FindTreeNode(object reference)
		{
			if (reference is ITypeDefOrRef)
				return FindTypeNode(((ITypeDefOrRef)reference).ResolveTypeDef());
			else if (reference is IMethod && ((IMethod)reference).MethodSig != null)
				return FindMethodNode(((IMethod)reference).Resolve());
			else if (reference is IField)
				return FindFieldNode(((IField)reference).Resolve());
			else if (reference is PropertyDef)
				return FindPropertyNode((PropertyDef)reference);
			else if (reference is EventDef)
				return FindEventNode((EventDef)reference);
			else if (reference is AssemblyDef)
				return FindAssemblyNode((AssemblyDef)reference);
			else if (reference is ModuleDef)
				return FindModuleNode((ModuleDef)reference);
			else
				return null;
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("asmlist"); }
		}
	}
}
