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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.TreeView;
using Microsoft.Win32;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Tree node representing an assembly.
	/// This class is responsible for loading both namespace and type nodes.
	/// </summary>
	public sealed class AssemblyTreeNode : ILSpyTreeNode
	{
		readonly LoadedAssembly assembly;
		readonly Dictionary<string, NamespaceTreeNode> namespaces = new Dictionary<string, NamespaceTreeNode>();

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

		public override object Text
		{
			get { return HighlightSearchMatch(assembly.ShortName); }
		}

		public override object Icon
		{
			get
			{
				if (assembly.IsLoaded) {
					return assembly.HasLoadError ? Images.AssemblyWarning : Images.Assembly;
				} else {
					return Images.AssemblyLoading;
				}
			}
		}

		public override bool ShowExpander
		{
			get { return !assembly.HasLoadError; }
		}

		void OnAssemblyLoaded(Task<AssemblyDefinition> assemblyTask)
		{
			// change from "Loading" icon to final icon
			RaisePropertyChanged("Icon");
			RaisePropertyChanged("ExpandedIcon");
			if (assemblyTask.IsFaulted) {
				RaisePropertyChanged("ShowExpander"); // cannot expand assemblies with load error
				// observe the exception so that the Task's finalizer doesn't re-throw it
				try { assemblyTask.Wait(); }
				catch (AggregateException) { }
			} else {
				RaisePropertyChanged("Text"); // shortname might have changed
			}
		}

		Dictionary<TypeDefinition, TypeTreeNode> typeDict = new Dictionary<TypeDefinition, TypeTreeNode>();

		protected override void LoadChildren()
		{
			AssemblyDefinition assemblyDefinition = assembly.AssemblyDefinition;
			if (assemblyDefinition == null) {
				// if we crashed on loading, then we don't have any children
				return;
			}
			ModuleDefinition mainModule = assemblyDefinition.MainModule;

			this.Children.Add(new ReferenceFolderTreeNode(mainModule, this));
			if (mainModule.HasResources)
				this.Children.Add(new ResourceListTreeNode(mainModule));
			foreach (NamespaceTreeNode ns in namespaces.Values) {
				ns.Children.Clear();
			}
			foreach (TypeDefinition type in mainModule.Types.OrderBy(t => t.FullName)) {
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
		
		public override bool CanExpandRecursively {
			get { return true; }
		}

		/// <summary>
		/// Finds the node for a top-level type.
		/// </summary>
		public TypeTreeNode FindTypeNode(TypeDefinition def)
		{
			if (def == null)
				return null;
			EnsureLazyChildren();
			TypeTreeNode node;
			if (typeDict.TryGetValue(def, out node))
				return node;
			else
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
			else
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
				if (ex.InnerException is BadImageFormatException) {
					language.WriteCommentLine(output, "This file does not contain a managed assembly.");
					return;
				} else {
					throw;
				}
			}
			language.DecompileAssembly(assembly, output, options);
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
	}

	[ExportContextMenuEntry(Header = "_Remove", Icon = "images/Delete.png")]
	sealed class RemoveAssembly : IContextMenuEntry
	{
		public bool IsVisible(SharpTreeNode[] selectedNodes)
		{
			return selectedNodes.All(n => n is AssemblyTreeNode);
		}

		public bool IsEnabled(SharpTreeNode[] selectedNodes)
		{
			return true;
		}

		public void Execute(SharpTreeNode[] selectedNodes)
		{
			foreach (var node in selectedNodes) {
				node.Delete();
			}
		}
	}
}
