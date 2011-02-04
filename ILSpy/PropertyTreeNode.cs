// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Represents a property in the TreeView.
	/// </summary>
	sealed class PropertyTreeNode : SharpTreeNode
	{
		readonly PropertyDefinition property;
		readonly bool isIndexer;
		
		public PropertyTreeNode(PropertyDefinition property, bool isIndexer)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			this.property = property;
			this.isIndexer = isIndexer;
			this.LazyLoading = true;
		}
		
		public override object Text {
			get { return property.Name + " : " + Language.Current.TypeToString(property.PropertyType); }
		}
		
		public override object Icon {
			get {
				return isIndexer ? Images.Indexer : Images.Property;
			}
		}
		
		protected override void LoadChildren()
		{
			if (property.GetMethod != null)
				this.Children.Add(new MethodTreeNode(property.GetMethod));
			if (property.SetMethod != null)
				this.Children.Add(new MethodTreeNode(property.SetMethod));
			if (property.HasOtherMethods) {
				foreach (var m in property.OtherMethods)
					this.Children.Add(new MethodTreeNode(m));
			}
		}
	}
}
