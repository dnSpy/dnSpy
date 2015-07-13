/*
    Copyright (C) 2014-2015 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes.Filters
{
	sealed class PublicApiTreeViewNodeFilter : ChainTreeViewNodeFilter
	{
		readonly Func<bool> showPublicApi;

		public PublicApiTreeViewNodeFilter(ITreeViewNodeFilter filter, Func<bool> showPublicApi)
			: base(filter)
		{
			if (showPublicApi == null)
				throw new ArgumentNullException();
			this.showPublicApi = showPublicApi;
		}

		public override TreeViewNodeFilterResult GetFilterResult(DerivedTypesEntryNode node)
		{
			if (showPublicApi() && !node.IsPublicAPI)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
			return base.GetFilterResult(node);
		}

		public override TreeViewNodeFilterResult GetFilterResult(EventDef evt)
		{
			if (showPublicApi() && !EventTreeNode.IsPublicAPIInternal(evt))
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
			return base.GetFilterResult(evt);
		}

		public override TreeViewNodeFilterResult GetFilterResult(FieldDef field)
		{
			if (showPublicApi() && !FieldTreeNode.IsPublicAPIInternal(field))
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
			return base.GetFilterResult(field);
		}

		public override TreeViewNodeFilterResult GetFilterResult(MethodDef method)
		{
			if (showPublicApi() && !MethodTreeNode.IsPublicAPIInternal(method))
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
			return base.GetFilterResult(method);
		}

		public override TreeViewNodeFilterResult GetFilterResult(PropertyDef prop)
		{
			if (showPublicApi() && !PropertyTreeNode.IsPublicAPIInternal(prop))
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
			return base.GetFilterResult(prop);
		}

		public override TreeViewNodeFilterResult GetFilterResult(TypeDef type)
		{
			if (showPublicApi() && !TypeTreeNode.IsPublicAPIInternal(type))
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
			return base.GetFilterResult(type);
		}

		public override TreeViewNodeFilterResult GetFilterResult(ResourceTreeNode node)
		{
			if (showPublicApi() && !ResourceTreeNode.IsPublicAPIInternal(node.Resource))
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
			return base.GetFilterResult(node);
		}
	}
}
