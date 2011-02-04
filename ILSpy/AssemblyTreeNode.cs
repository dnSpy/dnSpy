// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	sealed class AssemblyTreeNode : SharpTreeNode
	{
		readonly AssemblyListTreeNode assemblyList;
		readonly string fileName;
		string shortName;
		readonly Task<AssemblyDefinition> assemblyTask;
		readonly List<TypeTreeNode> classes = new List<TypeTreeNode>();
		readonly Dictionary<string, NamespaceTreeNode> namespaces = new Dictionary<string, NamespaceTreeNode>();
		readonly SynchronizationContext syncContext;
		
		public AssemblyTreeNode(string fileName, AssemblyListTreeNode assemblyList)
		{
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			
			this.fileName = fileName;
			this.assemblyList = assemblyList;
			this.assemblyTask = Task.Factory.StartNew<AssemblyDefinition>(LoadAssembly); // requires that this.fileName is set
			this.shortName = Path.GetFileNameWithoutExtension(fileName);
			this.syncContext = SynchronizationContext.Current;
			
			this.LazyLoading = true;
		}
		
		public string FileName {
			get { return fileName; }
		}
		
		public AssemblyDefinition AssemblyDefinition {
			get { return assemblyTask.Result; }
		}
		
		public override object Text {
			get { return shortName; }
		}
		
		public override object Icon {
			get { return Images.Assembly; }
		}
		
		AssemblyDefinition LoadAssembly()
		{
			// runs on background thread
			ReaderParameters p = new ReaderParameters();
			p.AssemblyResolver = new MyAssemblyResolver(this);
			AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(fileName, p);
			foreach (TypeDefinition type in assembly.MainModule.Types.OrderBy(t => t.FullName)) {
				classes.Add(new TypeTreeNode(type));
			}
			syncContext.Post(
				delegate {
					if (shortName != assembly.Name.Name) {
						shortName = assembly.Name.Name;
						RaisePropertyChanged("Text");
					}
				}, null);
			
			return assembly;
		}
		
		sealed class MyAssemblyResolver : IAssemblyResolver
		{
			readonly AssemblyTreeNode parent;
			
			public MyAssemblyResolver(AssemblyTreeNode parent)
			{
				this.parent = parent;
			}
			
			public AssemblyDefinition Resolve(AssemblyNameReference name)
			{
				var node = parent.LookupReferencedAssembly(name.FullName);
				return node != null ? node.AssemblyDefinition : null;
			}
			
			public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
			{
				var node = parent.LookupReferencedAssembly(name.FullName);
				return node != null ? node.AssemblyDefinition : null;
			}
			
			public AssemblyDefinition Resolve(string fullName)
			{
				var node = parent.LookupReferencedAssembly(fullName);
				return node != null ? node.AssemblyDefinition : null;
			}
			
			public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
			{
				var node = parent.LookupReferencedAssembly(fullName);
				return node != null ? node.AssemblyDefinition : null;
			}
		}
		
		protected override void LoadChildren()
		{
			assemblyTask.Wait();
			this.Children.Add(new ReferenceFolderTreeNode(assemblyTask.Result.MainModule, this));
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
		
		internal const string DataFormat = "ILSpyAssemblies";
		
		public override IDataObject Copy(SharpTreeNode[] nodes)
		{
			DataObject dataObject = new DataObject();
			dataObject.SetData(DataFormat, nodes.OfType<AssemblyTreeNode>().Select(n => n.fileName).ToArray());
			return dataObject;
		}
		
		public AssemblyTreeNode LookupReferencedAssembly(string fullName)
		{
			foreach (AssemblyTreeNode node in assemblyList.Children) {
				if (fullName.Equals(node.AssemblyDefinition.FullName, StringComparison.OrdinalIgnoreCase))
					return node;
			}
			
			var name = AssemblyNameReference.Parse(fullName);
			string file = GacInterop.FindAssemblyInNetGac(name);
			if (file == null) {
				string dir = Path.GetDirectoryName(this.fileName);
				if (File.Exists(Path.Combine(dir, name.Name + ".dll")))
					file = Path.Combine(dir, name.Name + ".dll");
				else if (File.Exists(Path.Combine(dir, name.Name + ".exe")))
					file = Path.Combine(dir, name.Name + ".exe");
			}
			if (file != null) {
				return assemblyList.OpenAssembly(file);
			} else {
				return null;
			}
		}
	}
}
