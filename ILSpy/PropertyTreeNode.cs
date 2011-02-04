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
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Represents a property in the TreeView.
	/// </summary>
	sealed class PropertyTreeNode : ILSpyTreeNode
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
		
		public override FilterResult Filter(FilterSettings settings)
		{
			if (settings.SearchTermMatches(property.Name))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}
	}
}
