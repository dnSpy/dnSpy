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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes {
	[Export(typeof(IResourceFactory<Resource, ResourceTreeNode>))]
	sealed class ResourceElementSetTreeNodeFactory : IResourceFactory<Resource, ResourceTreeNode> {
		public int Priority {
			get { return 100; }
		}

		public ResourceTreeNode Create(ModuleDef module, Resource resInput) {
			var er = resInput as EmbeddedResource;
			if (er == null)
				return null;
			er.Data.Position = 0;
			if (!dnlib.DotNet.Resources.ResourceReader.CouldBeResourcesFile(er.Data))
				return null;

			er.Data.Position = 0;
			return new ResourceElementSetTreeNode(module, er);
		}
	}

	sealed class ResourceElementSetTreeNode : ResourceTreeNode {
		readonly ResourceElementSet resourceElementSet;
		readonly ModuleDef module;

		public ResourceElementSetTreeNode(ModuleDef module, EmbeddedResource er)
			: base(er) {
			this.module = module;
			this.resourceElementSet = dnlib.DotNet.Resources.ResourceReader.Read(module, er.Data);
			this.LazyLoading = true;
		}

		internal ResourceElementSetTreeNode(ModuleDef module, string name, ManifestResourceAttributes flags)
			: base(new EmbeddedResource(name, new byte[0], flags)) {
			this.module = module;
			RegenerateEmbeddedResource(module);
			this.resourceElementSet = dnlib.DotNet.Resources.ResourceReader.Read(module, ((EmbeddedResource)r).Data);
			this.LazyLoading = true;
		}

		public override string IconName {
			get { return "ResourcesFile"; }
		}

		protected override IEnumerable<ResourceData> GetDeserialized() {
			EnsureChildrenFiltered();
			foreach (IResourceNode node in Children) {
				foreach (var data in node.GetResourceData(ResourceDataType.Deserialized))
					yield return data;
			}
		}

		protected override void LoadChildren() {
			var ary = resourceElementSet.ResourceElements.ToArray();
			Array.Sort(ary, ResourceElementComparer.Instance);
			foreach (var elem in ary)
				Children.Add(ResourceFactory.Create(module, elem));
		}

		protected override int GetNewChildIndex(SharpTreeNode node) {
			if (node is ResourceElementTreeNode)
				return GetNewChildIndex(node, (a, b) => ResourceElementComparer.Instance.Compare(((ResourceElementTreeNode)a).ResourceElement, ((ResourceElementTreeNode)b).ResourceElement));
			return base.GetNewChildIndex(node);
		}

		protected override bool SortOnNodeType {
			get { return false; }
		}

		public override void Decompile(Language language, ITextOutput output) {
			App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(EnsureChildrenFiltered));

			base.Decompile(language, output);
			var so = output as ISmartTextOutput;
			if (so != null) {
				so.AddButton(null, "Save", (s, e) => Save());
				so.WriteLine();
				so.WriteLine();
			}

			foreach (ResourceElementTreeNode child in Children)
				child.Decompile(language, output);
		}

		public override void RegenerateEmbeddedResource() {
			var module = GetModule(this);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();
			RegenerateEmbeddedResource(module);
		}

		void RegenerateEmbeddedResource(ModuleDef module) {
			EnsureChildrenFiltered();
			var outStream = new MemoryStream();
			var resources = new ResourceElementSet();
			foreach (ResourceElementTreeNode child in Children)
				resources.Add(child.ResourceElement);
			ResourceWriter.Write(module, outStream, resources);
			this.r = new EmbeddedResource(r.Name, outStream.ToArray(), r.Attributes);
		}

		public override bool Save(TextView.DecompilerTextView textView) {
			Save();
			return true;
		}
	}
}
