// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	sealed class AssemblyTreeNode : SharpTreeNode
	{
		readonly string fileName;
		readonly string name;
		readonly Task<AssemblyDefinition> assemblyTask;
		readonly List<TypeTreeNode> classes = new List<TypeTreeNode>();
		readonly Dictionary<string, NamespaceTreeNode> namespaces = new Dictionary<string, NamespaceTreeNode>();
		
		public AssemblyTreeNode(string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			
			this.fileName = fileName;
			this.assemblyTask = Task.Factory.StartNew<AssemblyDefinition>(LoadAssembly); // requires that this.fileName is set
			this.name = Path.GetFileNameWithoutExtension(fileName);
			
			this.LazyLoading = true;
		}
		
		public override object Text {
			get { return name; }
		}
		
		public override object Icon {
			get { return Images.Assembly; }
		}
		
		AssemblyDefinition LoadAssembly()
		{
			// runs on background thread
			AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(fileName);
			foreach (TypeDefinition type in assembly.MainModule.Types.OrderBy(t => t.FullName)) {
				classes.Add(new TypeTreeNode(type));
			}
			return assembly;
		}
		
		protected override void LoadChildren()
		{
			assemblyTask.Wait();
			
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
		
		/// <summary>
		/// Invalidates the list of children.
		/// </summary>
		void InvalidateChildren()
		{
			this.Children.Clear();
			if (this.IsExpanded)
				this.LoadChildren();
			else
				this.LazyLoading = true;
		}
		
		bool showInternalAPI = true;
		
		public bool ShowInternalAPI {
			get { return showInternalAPI; }
			set {
				if (showInternalAPI != value) {
					showInternalAPI = value;
					InvalidateChildren();
					RaisePropertyChanged("ShowInternalAPI");
				}
			}
		}
		
		public override bool CanDrag(SharpTreeNode[] nodes)
		{
			return nodes.All(n => n is AssemblyTreeNode);
		}
		
		public override bool CanDelete(SharpTreeNode[] nodes)
		{
			return Parent != null && Parent.CanDelete(nodes); // handle deletion in the AssemblyListTreeNode
		}
		
		public override void Delete(SharpTreeNode[] nodes)
		{
			Parent.Delete(nodes); // handle deletion in the AssemblyListTreeNode
		}
		
		public override void DeleteCore(SharpTreeNode[] nodes)
		{
			Parent.DeleteCore(nodes); // handle deletion in the AssemblyListTreeNode
		}
		
		public override IDataObject Copy(SharpTreeNode[] nodes)
		{
			DataObject dataObject = new DataObject();
			dataObject.SetData("ILSpyAssemblies", nodes.OfType<AssemblyTreeNode>());
			return dataObject;
		}
	}
}
