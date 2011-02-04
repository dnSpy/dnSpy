// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	sealed class AssemblyTreeNode : SharpTreeNode
	{
		readonly AssemblyDefinition assembly;
		readonly List<TypeTreeNode> classes = new List<TypeTreeNode>();
		public ObservableCollection<NamespaceTreeNode> Namespaces { get; private set; }
		readonly Dictionary<TypeDefinition, TypeTreeNode> typeDict = new Dictionary<TypeDefinition, TypeTreeNode>();
		readonly Dictionary<string, NamespaceTreeNode> namespaces = new Dictionary<string, NamespaceTreeNode>();
		
		public AssemblyTreeNode(AssemblyDefinition assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");
			this.assembly = assembly;
			this.Namespaces = new ObservableCollection<NamespaceTreeNode>();
			InitClassList();
			BuildNestedTypeLists();
			BuildNamespaceList();
		}
		
		public override object Text {
			get { return assembly.Name.Name; }
		}
		
		public override object Icon {
			get { return Images.Assembly; }
		}
		
		void InitClassList()
		{
			foreach (TypeDefinition type in assembly.MainModule.Types.OrderBy(t => t.FullName)) {
				var viewmodel = new TypeTreeNode(type);
				typeDict.Add(type, viewmodel);
				if (type.IsNested == false) {
					classes.Add(viewmodel);
				}
				foreach (TypeDefinition nestedType in TreeTraversal.PreOrder(type.NestedTypes, t => t.NestedTypes)) {
					typeDict.Add(nestedType, new TypeTreeNode(nestedType));
				}
			}
		}
		
		void BuildNestedTypeLists()
		{
			foreach (var typeModel in typeDict.Values) {
				typeModel.NestedTypes.Clear();
			}
			foreach (var pair in typeDict) {
				if (showInternalAPI == false && pair.Value.IsPublicAPI == false)
					continue;
				if (pair.Key.IsNested) {
					typeDict[pair.Key.DeclaringType].NestedTypes.Add(pair.Value);
				}
			}
		}
		
		void BuildNamespaceList()
		{
			this.Children.Clear();
			foreach (NamespaceTreeNode ns in namespaces.Values) {
				ns.Children.Clear();
			}
			foreach (TypeTreeNode type in classes) {
				if (showInternalAPI == false && type.IsPublicAPI == false)
					continue;
				NamespaceTreeNode ns;
				if (!namespaces.TryGetValue(type.Namespace, out ns)) {
					ns = new NamespaceTreeNode(type.Namespace);
					namespaces[type.Namespace] = ns;
				}
				ns.Children.Add(type);
			}
			foreach (NamespaceTreeNode ns in namespaces.Values.OrderBy(n => n.Name)) {
				if (ns.Children.Count > 0)
					this.Children.Add(ns);
			}
		}
		
		bool showInternalAPI;
		
		public bool ShowInternalAPI {
			get { return showInternalAPI; }
			set {
				if (showInternalAPI != value) {
					showInternalAPI = value;
					BuildNestedTypeLists();
					BuildNamespaceList();
					RaisePropertyChanged("ShowInternalAPI");
				}
			}
		}
	}
}
