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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using dnlib.DotNet;
using dnSpy;
using dnSpy.Decompiler;
using dnSpy.Files;
using dnSpy.NRefactory;
using dnSpy.TreeNodes;
using dnSpy.TreeNodes.Hex;
using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes {
	/// <summary>
	/// Represents a list of files.
	/// This is used as (invisible) root node of the tree view.
	/// </summary>
	sealed class DnSpyFileListTreeNode : ILSpyTreeNode
	{
		readonly DnSpyFileList dnspyFileList;

		public object OwnerTreeView { get; set; }

		public DnSpyFileListTreeNode(DnSpyFileList dnspyFileList)
		{
			if (dnspyFileList == null)
				throw new ArgumentNullException("dnspyFileList");
			this.dnspyFileList = dnspyFileList;
			BindToObservableCollection();
		}

		protected override void Write(ITextOutput output, Language language)
		{
			output.Write(UIUtils.CleanUpName(dnspyFileList.Name), TextTokenType.Text);
		}

		void BindToObservableCollection()
		{
			this.Children.Clear();
			this.Children.AddRange(dnspyFileList.GetDnSpyFiles().Select(a => CreateAssemblyTreeNode(a)));
			dnspyFileList.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e) {
				switch (e.Action) {
					case NotifyCollectionChangedAction.Add:
					this.Children.InsertRange(e.NewStartingIndex, e.NewItems.Cast<DnSpyFile>().Select(a => CreateAssemblyTreeNode(a)));
						break;
					case NotifyCollectionChangedAction.Remove:
						this.Children.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
						break;
					case NotifyCollectionChangedAction.Replace:
					case NotifyCollectionChangedAction.Move:
						throw new NotImplementedException();
					case NotifyCollectionChangedAction.Reset:
						this.Children.Clear();
						this.Children.AddRange(dnspyFileList.GetDnSpyFiles().Select(a => CreateAssemblyTreeNode(a)));
						break;
					default:
						throw new NotSupportedException("Invalid value for NotifyCollectionChangedAction");
				}
			};
		}

		AssemblyTreeNode CreateAssemblyTreeNode(DnSpyFile file)
		{
			CachedAssemblyTreeNode cachedInfo;
			if (cachedAsmTreeNodes.TryGetValue(file, out cachedInfo)) {
				var asmNode = cachedInfo.AssemblyTreeNode;
				Debug.Assert(asmNode.Parent == null);
				if (asmNode.Parent != null)
					throw new InvalidOperationException();
				return asmNode;
			}
			return new AssemblyTreeNode(file);
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

		readonly Dictionary<DnSpyFile, CachedAssemblyTreeNode> cachedAsmTreeNodes = new Dictionary<DnSpyFile, CachedAssemblyTreeNode>();

		public void RegisterCached(DnSpyFile file, AssemblyTreeNode asmNode)
		{
			CachedAssemblyTreeNode cachedInfo;
			if (!cachedAsmTreeNodes.TryGetValue(file, out cachedInfo))
				cachedAsmTreeNodes.Add(file, cachedInfo = new CachedAssemblyTreeNode(asmNode));
			else {
				Debug.Assert(cachedInfo.AssemblyTreeNode == asmNode);
				if (cachedInfo.AssemblyTreeNode != asmNode)
					throw new InvalidOperationException();
			}
			cachedInfo.Counter++;
		}

		public void UnregisterCached(DnSpyFile asm)
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
				DnSpyFile newSelectedAsm = null;
				bool newSelectedAsmExisted = false;
				var oldIgnoreSelChg = MainWindow.Instance.TreeView_SelectionChanged_ignore;
				try {
					lock (dnspyFileList.GetLockObj()) {
						int numFiles = dnspyFileList.Count_NoLock;
						var old = dnspyFileList.IsReArranging;
						try {
							MainWindow.Instance.TreeView_SelectionChanged_ignore = true;
							var assemblies = (from file in files
											  where file != null
											  select dnspyFileList.OpenFile(file) into node
											  where node != null
											  select node).Distinct().ToList();
							var oldAsm = new Dictionary<DnSpyFile, bool>(assemblies.Count);
							foreach (var asm in assemblies) {
								int nodeIndex = dnspyFileList.IndexOf_NoLock(asm);
								oldAsm[asm] = nodeIndex < numFiles;
								if (newSelectedAsm == null) {
									newSelectedAsm = asm;
									newSelectedAsmExisted = oldAsm[asm];
								}
								if (nodeIndex < index)
									index--;
								numFiles--;
								dnspyFileList.IsReArranging = oldAsm[asm];
								dnspyFileList.RemoveAt_NoLock(nodeIndex);
								dnspyFileList.IsReArranging = old;
							}
							assemblies.Reverse();
							foreach (var asm in assemblies) {
								dnspyFileList.IsReArranging = oldAsm[asm];
								dnspyFileList.Insert_NoLock(index, asm);
								dnspyFileList.IsReArranging = old;
							}
						}
						finally {
							dnspyFileList.IsReArranging = old;
						}
					}
					if (newSelectedAsm != null) {
						if (!newSelectedAsmExisted)
							MainWindow.Instance.TreeView_SelectionChanged_ignore = oldIgnoreSelChg;
						var node = MainWindow.Instance.FindTreeNode(newSelectedAsm.AssemblyDef) ??
							MainWindow.Instance.FindTreeNode(newSelectedAsm.ModuleDef);
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
			language.WriteCommentLine(output, "List: " + dnspyFileList.Name);
			output.WriteLine();
			foreach (AssemblyTreeNode asm in this.Children) {
				language.WriteCommentLine(output, new string('-', 60));
				output.WriteLine();
				asm.Decompile(language, output, options);
			}
		}

		public SerializedDnSpyModule GetSerializedDnSpyModule(ModuleDef module) {
			if (module == null)
				return new SerializedDnSpyModule();
			var modNode = MainWindow.Instance.DnSpyFileListTreeNode.FindModuleNode(module);
			if (modNode == null)
				return SerializedDnSpyModule.CreateFromFile(module);
			return modNode.DnSpyFile.SerializedDnSpyModule ?? SerializedDnSpyModule.CreateFromFile(module);
		}

		#region Find*Node

		public IEnumerable<AssemblyTreeNode> GetAllModuleNodes() {
			App.Current.Dispatcher.VerifyAccess();
			foreach (AssemblyTreeNode node in this.Children) {
				if (!node.IsDotNetFile)
					continue;
				if (node.IsNetModule)
					yield return node;
				else {
					node.EnsureChildrenFiltered();
					foreach (var asmNode in node.Children.OfType<AssemblyTreeNode>())
						yield return asmNode;
				}
			}
		}

		public AssemblyTreeNode FindModuleNode(ModuleDef module)
		{
			if (module == null)
				return null;
			App.Current.Dispatcher.VerifyAccess();
			foreach (AssemblyTreeNode node in this.Children) {
				if (!node.IsDotNetFile)
					continue;
				if (node.IsNetModule) {
					if (node.DnSpyFile.ModuleDef == module)
						return node;
				}
				else {
					node.EnsureChildrenFiltered();
					foreach (var asmNode in node.Children.OfType<AssemblyTreeNode>()) {
						if (asmNode.DnSpyFile.ModuleDef == module)
							return asmNode;
					}
				}
			}
			return null;
		}

		internal MetaDataTableRecordTreeNode FindTokenNode(TokenReference @ref)
		{
			var modNode = FindModuleNode(@ref.ModuleDef);
			return modNode == null ? null : modNode.FindTokenNode(@ref.Token);
		}

		public AssemblyTreeNode FindAssemblyNode(AssemblyDef asm)
		{
			if (asm == null)
				return null;
			App.Current.Dispatcher.VerifyAccess();
			foreach (AssemblyTreeNode node in this.Children) {
				if (node.DnSpyFile.AssemblyDef == asm)
					return node;
			}
			return null;
		}

		public AssemblyTreeNode FindAssemblyNode(DnSpyFile asm)
		{
			if (asm == null)
				return null;
			App.Current.Dispatcher.VerifyAccess();
			foreach (AssemblyTreeNode node in this.Children) {
				if (node.DnSpyFile == asm)
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
					return decl.Children.OfType<TypeTreeNode>().FirstOrDefault(t => t.TypeDef == def);
				}
			} else {
				AssemblyTreeNode asm = FindModuleNode(def.Module);
				if (asm != null) {
					return asm.FindNonNestedTypeNode(def);
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
			MethodTreeNode methodNode = typeNode.Children.OfType<MethodTreeNode>().FirstOrDefault(m => m.MethodDef == def);
			if (methodNode != null)
				return methodNode;
			foreach (var p in typeNode.Children.OfType<ILSpyTreeNode>()) {
				if (p is PropertyTreeNode || p is EventTreeNode) {
					p.EnsureChildrenFiltered();
					methodNode = p.Children.OfType<MethodTreeNode>().FirstOrDefault(m => m.MethodDef == def);
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
			return typeNode.Children.OfType<FieldTreeNode>().FirstOrDefault(m => m.FieldDef == def);
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
			return typeNode.Children.OfType<PropertyTreeNode>().FirstOrDefault(m => m.PropertyDef == def);
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
			return typeNode.Children.OfType<EventTreeNode>().FirstOrDefault(m => m.EventDef == def);
		}
		#endregion

		public DnSpyFile FindModule(DnSpyFile asm, string moduleFilename)
		{
			App.Current.Dispatcher.VerifyAccess();
			foreach (AssemblyTreeNode node in this.Children) {
				if (node.DnSpyFile != asm)
					continue;
				if (!node.IsDotNetFile || node.IsNetModule)
					continue;

				node.EnsureChildrenFiltered();
				foreach (var asmNode in node.Children.OfType<AssemblyTreeNode>()) {
					if (string.IsNullOrWhiteSpace(asmNode.DnSpyFile.Filename))
						continue;
					if (asmNode.DnSpyFile.Filename.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
						return asmNode.DnSpyFile;
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
			else if (reference is ILSpyTreeNode)
				return (ILSpyTreeNode)reference;
			else if (reference is TokenReference)
				return FindTokenNode((TokenReference)reference);
			else
				return null;
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("asmlist"); }
		}
	}
}
