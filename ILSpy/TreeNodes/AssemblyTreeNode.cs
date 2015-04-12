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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.Options;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.TreeView;
using Microsoft.Win32;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Tree node representing an assembly.
	/// This class is responsible for loading both namespace and type nodes.
	/// </summary>
	public sealed class AssemblyTreeNode : ILSpyTreeNode
	{
		readonly LoadedAssembly assembly;
		readonly Dictionary<string, NamespaceTreeNode> namespaces = new Dictionary<string, NamespaceTreeNode>(StringComparer.Ordinal);

		public AssemblyTreeNode(LoadedAssembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");

			this.assembly = assembly;

			assembly.ContinueWhenLoaded(OnAssemblyLoaded, TaskScheduler.FromCurrentSynchronizationContext());

			this.LazyLoading = true;
		}

		public AssemblyList AssemblyList
		{
			get { return assembly.AssemblyList; }
		}

		public LoadedAssembly LoadedAssembly
		{
			get { return assembly; }
		}

		/// <summary>
		/// true if this is a netmodule (it doesn't have an assembly)
		/// </summary>
		public bool IsNetModule
		{
			get { return assembly.AssemblyDefinition == null; }
		}

		public override bool IsAutoLoaded
		{
			get { 
				return assembly.IsAutoLoaded; 
			}
		}

		public override object Text
		{
			get { return ToString(Language); }
		}

		public override string ToString(Language language)
		{
			if (!assembly.IsLoaded)
				return assembly.ShortName;
			if (assembly.ModuleDefinition == null)
				return CleanUpName(assembly.ShortName);
			if (Parent is AssemblyTreeNode || assembly.AssemblyDefinition == null)
				return CleanUpName(assembly.ModuleDefinition.Name);
			return CleanUpName(GetAssemblyName());
		}

		string GetAssemblyName()
		{
			var asm = assembly.AssemblyDefinition;
			int numExtra = (DisplaySettingsPanel.CurrentDisplaySettings.ShowAssemblyVersion ? 1 : 0) +
				(DisplaySettingsPanel.CurrentDisplaySettings.ShowAssemblyPublicKeyToken ? 1 : 0);
			if (numExtra == 0)
				return asm.Name;

			var sb = new StringBuilder(asm.Name.Length + 3 /* " ()" */ + (numExtra - 1) * 2 /* ", " */ +
					(DisplaySettingsPanel.CurrentDisplaySettings.ShowAssemblyVersion ? 5 * 4 + 3 : 0) +
					(DisplaySettingsPanel.CurrentDisplaySettings.ShowAssemblyPublicKeyToken ? 8 * 2 : 0));
			sb.Append(asm.Name);
			sb.Append(" (");
			int len = sb.Length;
			if (DisplaySettingsPanel.CurrentDisplaySettings.ShowAssemblyVersion) {
				if (len != sb.Length)
					sb.Append(", ");
				sb.Append(asm.Version.ToString());
			}
			if (DisplaySettingsPanel.CurrentDisplaySettings.ShowAssemblyPublicKeyToken && !PublicKeyBase.IsNullOrEmpty2(asm.PublicKeyToken)) {
				if (len != sb.Length)
					sb.Append(", ");
				sb.Append(asm.PublicKeyToken.ToString());
			}
			sb.Append(')');
			return sb.ToString();
		}

		public override object Icon
		{
			get
			{
				if (assembly.IsLoaded) {
					if (assembly.HasLoadError)
						return Images.AssemblyWarning;
					if (Parent is AssemblyTreeNode || (assembly.ModuleDefinition != null && assembly.ModuleDefinition.Kind == ModuleKind.NetModule))
						return Images.AssemblyModule;
					return assembly.ModuleDefinition != null &&
						assembly.ModuleDefinition.IsManifestModule &&
						(assembly.ModuleDefinition.Kind == ModuleKind.Console ||
						assembly.ModuleDefinition.Kind == ModuleKind.Windows) ?
						Images.AssemblyExe : Images.Assembly;
				} else {
					return Images.AssemblyLoading;
				}
			}
		}

		public override object ToolTip
		{
			get {
				if (!assembly.IsLoaded)
					return assembly.FileName;
				if (assembly.HasLoadError)
					return "Assembly could not be loaded.";

				var tooltip = new TextBlock();
				tooltip.Inlines.Add(new Bold(new Run("Name: ")));
				var name = Parent is AssemblyTreeNode || assembly.AssemblyDefinition == null ?
							assembly.ModuleDefinition.Name.String :
							assembly.AssemblyDefinition.FullName;
				tooltip.Inlines.Add(new Run(name));
				tooltip.Inlines.Add(new LineBreak());
				tooltip.Inlines.Add(new Bold(new Run("Location: ")));
				tooltip.Inlines.Add(new Run(assembly.FileName));
				tooltip.Inlines.Add(new LineBreak());
				tooltip.Inlines.Add(new Bold(new Run("Architecture: ")));
				tooltip.Inlines.Add(new Run(CSharpLanguage.GetPlatformDisplayName(assembly.ModuleDefinition)));
				string runtimeName = CSharpLanguage.GetRuntimeDisplayName(assembly.ModuleDefinition);
				if (runtimeName != null) {
					tooltip.Inlines.Add(new LineBreak());
					tooltip.Inlines.Add(new Bold(new Run("Runtime: ")));
					tooltip.Inlines.Add(new Run(runtimeName));
				}

				return tooltip;
			}
		}

		public override bool ShowExpander
		{
			get { return !assembly.HasLoadError; }
		}

		void OnAssemblyLoaded(Task<ModuleDef> moduleTask)
		{
			// change from "Loading" icon to final icon
			RaisePropertyChanged("Icon");
			RaisePropertyChanged("ExpandedIcon");
			RaisePropertyChanged("ToolTip");
			if (moduleTask.IsFaulted) {
				RaisePropertyChanged("ShowExpander"); // cannot expand assemblies with load error
				// observe the exception so that the Task's finalizer doesn't re-throw it
				try { moduleTask.Wait(); }
				catch (AggregateException) { }
			}
			RaisePropertyChanged("Text");
		}

		readonly Dictionary<TypeDef, TypeTreeNode> typeDict = new Dictionary<TypeDef, TypeTreeNode>();

		protected override void LoadChildren()
		{
			ModuleDef moduleDefinition = assembly.ModuleDefinition;
			Debug.Assert(moduleDefinition != null, "dnSpy crashed on loading");
			if (moduleDefinition == null) {
				// if we crashed on loading, then we don't have any children
				return;
			}

			if (Parent is AssemblyTreeNode || assembly.AssemblyDefinition == null) {
				LoadModuleChildren(moduleDefinition);
			}
			else {
				// Add all modules in this assembly
				foreach (var mod in assembly.AssemblyDefinition.Modules) {
					if (mod == assembly.ModuleDefinition)
						this.Children.Add(new AssemblyTreeNode(assembly));
					else {
						var loadAsm = new LoadedAssembly(AssemblyList, mod);
						loadAsm.IsAutoLoaded = assembly.IsAutoLoaded;
						this.Children.Add(new AssemblyTreeNode(loadAsm));
					}
				}
			}
		}

		void LoadModuleChildren(ModuleDef moduleDefinition)
		{
			if (moduleDefinition is ModuleDefMD)
				this.Children.Add(new ReferenceFolderTreeNode((ModuleDefMD)moduleDefinition, this));
			if (moduleDefinition.HasResources)
				this.Children.Add(new ResourceListTreeNode(moduleDefinition));
			foreach (NamespaceTreeNode ns in namespaces.Values) {
				ns.Children.Clear();
			}
			foreach (TypeDef type in moduleDefinition.Types.OrderBy(t => t.FullName)) {
				NamespaceTreeNode ns;
				if (!namespaces.TryGetValue(type.Namespace, out ns)) {
					ns = new NamespaceTreeNode(type.Namespace);
					namespaces[type.Namespace] = ns;
				}
				TypeTreeNode node = new TypeTreeNode(type, this);
				typeDict[type] = node;
				ns.Children.Add(node);
			}
			foreach (NamespaceTreeNode ns in namespaces.Values.OrderBy(n => n.Name)) {
				if (ns.Children.Count > 0)
					this.Children.Add(ns);
			}
		}

		internal void OnRemoved(NamespaceTreeNode nsNode)
		{
			bool b = namespaces.Remove(nsNode.Name);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();

			foreach (TypeTreeNode typeNode in nsNode.Children)
				OnRemoved(typeNode);
		}

		internal void OnReadded(NamespaceTreeNode nsNode)
		{
			Debug.Assert(!namespaces.ContainsKey(nsNode.Name));
			namespaces.Add(nsNode.Name, nsNode);

			foreach (TypeTreeNode typeNode in nsNode.Children)
				OnReadded(typeNode);
		}

		internal void OnRemoved(TypeTreeNode typeNode)
		{
			bool b = typeDict.Remove(typeNode.TypeDefinition);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		internal void OnReadded(TypeTreeNode typeNode)
		{
			Debug.Assert(!typeDict.ContainsKey(typeNode.TypeDefinition));
			typeDict.Add(typeNode.TypeDefinition, typeNode);
		}

		public override bool CanExpandRecursively {
			get { return true; }
		}

		/// <summary>
		/// Finds the node for a top-level type.
		/// </summary>
		public TypeTreeNode FindTypeNode(TypeDef def)
		{
			if (def == null)
				return null;
			EnsureLazyChildren();
			TypeTreeNode node;
			if (typeDict.TryGetValue(def, out node))
				return node;

			if (!(Parent is AssemblyTreeNode)) {
				foreach (var asmNode in Children.OfType<AssemblyTreeNode>()) {
					node = asmNode.FindTypeNode(def);
					if (node != null)
						return node;
				}
			}
			Debug.Fail("Could not find type node");
			return null;
		}

		/// <summary>
		/// Finds the node for a namespace.
		/// </summary>
		public NamespaceTreeNode FindNamespaceNode(string namespaceName)
		{
			if (string.IsNullOrEmpty(namespaceName))
				return null;
			EnsureLazyChildren();
			NamespaceTreeNode node;
			if (namespaces.TryGetValue(namespaceName, out node))
				return node;
			Debug.Fail("Could not find namespace node");
			return null;
		}
		
		public override bool CanDrag(SharpTreeNode[] nodes)
		{
			return nodes.All(n => n is AssemblyTreeNode);
		}

		public override void StartDrag(DependencyObject dragSource, SharpTreeNode[] nodes)
		{
			DragDrop.DoDragDrop(dragSource, Copy(nodes), DragDropEffects.All);
		}

		public override bool CanDelete()
		{
			return true;
		}

		public override void Delete()
		{
			DeleteCore();
		}

		public override void DeleteCore()
		{
			assembly.AssemblyList.Unload(assembly);
		}

		internal const string DataFormat = "ILSpyAssemblies";

		public override IDataObject Copy(SharpTreeNode[] nodes)
		{
			DataObject dataObject = new DataObject();
			dataObject.SetData(DataFormat, nodes.OfType<AssemblyTreeNode>().Select(n => n.LoadedAssembly.FileName).ToArray());
			return dataObject;
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			if (settings.SearchTermMatches(assembly.ShortName))
				return FilterResult.Match;
			else
				return FilterResult.Recurse;
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			try {
				assembly.WaitUntilLoaded(); // necessary so that load errors are passed on to the caller
			} catch (AggregateException ex) {
				language.WriteCommentLine(output, assembly.FileName);
				if (ex.InnerException is BadImageFormatException || ex.InnerException is IOException) {
					language.WriteCommentLine(output, "This file does not contain a managed assembly.");
					return;
				} else {
					throw;
				}
			}
			var flags = Parent is AssemblyTreeNode ? DecompileAssemblyFlags.Module : DecompileAssemblyFlags.Assembly;
			if (assembly.AssemblyDefinition == null)
				flags = DecompileAssemblyFlags.Module;
			if (options.FullDecompilation)
				flags = DecompileAssemblyFlags.AssemblyAndModule;
			language.DecompileAssembly(assembly, output, options, flags);
		}

		public override bool Save(DecompilerTextView textView)
		{
			Language language = this.Language;
			if (string.IsNullOrEmpty(language.ProjectFileExtension))
				return false;
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.FileName = DecompilerTextView.CleanUpName(assembly.ShortName) + language.ProjectFileExtension;
			dlg.Filter = language.Name + " project|*" + language.ProjectFileExtension + "|" + language.Name + " single file|*" + language.FileExtension + "|All files|*.*";
			if (dlg.ShowDialog() == true) {
				DecompilationOptions options = new DecompilationOptions();
				options.FullDecompilation = true;
				if (dlg.FilterIndex == 1) {
					options.SaveAsProjectDirectory = Path.GetDirectoryName(dlg.FileName);
					foreach (string entry in Directory.GetFileSystemEntries(options.SaveAsProjectDirectory)) {
						if (!string.Equals(entry, dlg.FileName, StringComparison.OrdinalIgnoreCase)) {
							var result = MessageBox.Show(
								"The directory is not empty. File will be overwritten." + Environment.NewLine +
								"Are you sure you want to continue?",
								"Project Directory not empty",
								MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
							if (result == MessageBoxResult.No)
								return true; // don't save, but mark the Save operation as handled
							break;
						}
					}
				}
				textView.SaveToDisk(language, new[] { this }, options, dlg.FileName);
			}
			return true;
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("asm", assembly.FileName.ToUpperInvariant()); }
		}
	}

	[ExportContextMenuEntryAttribute(Header = "_Remove", Icon = "images/Delete.png", Order = 940, Category = "Other")]
	sealed class RemoveAssembly : IContextMenuEntry2
	{
		public void Initialize(TextViewContext context, MenuItem menuItem)
		{
			if (context.SelectedTreeNodes.Length == 1)
				menuItem.Header = string.Format("_Remove {0}", ((AssemblyTreeNode)context.SelectedTreeNodes[0]).LoadedAssembly.ShortName);
			else
				menuItem.Header = string.Format("_Remove {0} assemblies", context.SelectedTreeNodes.Length);
		}

		public bool IsVisible(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;
			return context.SelectedTreeNodes.Length > 0 && context.SelectedTreeNodes.All(n => n is AssemblyTreeNode);
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			foreach (var node in context.SelectedTreeNodes) {
				node.Delete();
			}
		}
	}

	[ExportContextMenuEntryAttribute(Header = "_Load Dependencies", Order = 930, Category = "Other")]
	sealed class LoadDependencies : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;
			return context.SelectedTreeNodes.Length > 0 && context.SelectedTreeNodes.All(n => n is AssemblyTreeNode);
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			foreach (var node in context.SelectedTreeNodes) {
				var la = ((AssemblyTreeNode)node).LoadedAssembly;
				if (!la.HasLoadError && la.ModuleDefinition is ModuleDefMD) {
					foreach (var assyRef in ((ModuleDefMD)la.ModuleDefinition).GetAssemblyRefs()) {
						la.LookupReferencedAssembly(assyRef);
					}
				}
			}
		}
	}

	[ExportContextMenuEntryAttribute(Header = "_Add to Main List", Order = 950, Category = "Other")]
	sealed class AddToMainList : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;
			return context.SelectedTreeNodes.Where(n => n is AssemblyTreeNode).Any(n=>((AssemblyTreeNode)n).IsAutoLoaded);
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			foreach (var node in context.SelectedTreeNodes) {
				foreach (var asmNode in GetAllRelatedNodes((AssemblyTreeNode)node)) {
					var loadedAsm = asmNode.LoadedAssembly;
					if (!loadedAsm.HasLoadError) {
						loadedAsm.IsAutoLoaded = false;
						asmNode.RaisePropertyChanged("Foreground");
					}
				}
			}
			MainWindow.Instance.CurrentAssemblyList.RefreshSave();
		}

		static IEnumerable<AssemblyTreeNode> GetAllRelatedNodes(AssemblyTreeNode node)
		{
			if (node.Parent is AssemblyTreeNode)
				node = (AssemblyTreeNode)node.Parent;
			yield return node;
			foreach (var child in node.Children) {
				var asmChild = child as AssemblyTreeNode;
				if (asmChild != null)
					yield return asmChild;
			}
		}
	}
}
