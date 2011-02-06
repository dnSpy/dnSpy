// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Lists the embedded resources in an assembly.
	/// </summary>
	class ResourceListTreeNode : ILSpyTreeNode<ResourceTreeNode>
	{
		readonly ModuleDefinition module;
		
		public ResourceListTreeNode(ModuleDefinition module)
		{
			this.LazyLoading = true;
			this.module = module;
		}
		
		public override object Text {
			get { return "Resources"; }
		}
		
		public override object Icon {
			get { return Images.Resource; }
		}
		
		protected override void LoadChildren()
		{
			foreach (Resource r in module.Resources)
				this.Children.Add(new ResourceTreeNode(r));
		}
		
		public override FilterResult Filter(FilterSettings settings)
		{
			if (string.IsNullOrEmpty(settings.SearchTerm))
				return FilterResult.MatchAndRecurse;
			else
				return FilterResult.Recurse;
		}
	}
	
	class ResourceTreeNode : ILSpyTreeNode<ILSpyTreeNodeBase>
	{
		Resource r;
		
		public ResourceTreeNode(Resource r)
		{
			this.r = r;
		}
		
		public override object Text {
			get { return r.Name; }
		}
		
		public override object Icon {
			get { return Images.Resource; }
		}
		
		public override FilterResult Filter(FilterSettings settings)
		{
			if (!settings.ShowInternalApi && (r.Attributes & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Private)
				return FilterResult.Hidden;
			if (settings.SearchTermMatches(r.Name))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}
	}
}
