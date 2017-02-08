/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Contracts.Settings.HexGroups {
	/// <summary>
	/// Provides <see cref="TagOptionDefinition"/>s. Use <see cref="ExportTagOptionDefinitionProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class TagOptionDefinitionProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected TagOptionDefinitionProvider() { }

		/// <summary>
		/// Returns the options
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerable<TagOptionDefinition> GetOptions();

		/// <summary>
		/// Gets the sub group (eg. <see cref="PredefinedHexViewRoles.HexEditorGroupDefault"/>) to use or null
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <returns></returns>
		public abstract string GetSubGroup(WpfHexView hexView);
	}

	/// <summary>Metadata</summary>
	public interface ITagOptionDefinitionProviderMetadata {
		/// <summary>See <see cref="ExportTagOptionDefinitionProviderAttribute.Order"/></summary>
		double Order { get; }
		/// <summary>See <see cref="ExportTagOptionDefinitionProviderAttribute.Group"/></summary>
		string Group { get; }
	}

	/// <summary>
	/// Exports a <see cref="TagOptionDefinitionProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportTagOptionDefinitionProviderAttribute : ExportAttribute, ITagOptionDefinitionProviderMetadata {
		/// <summary>Constructor</summary>
		/// <param name="group">Group, eg. <see cref="PredefinedHexViewGroupNames.HexEditor"/></param>
		/// <param name="order">Order of this instanec</param>
		public ExportTagOptionDefinitionProviderAttribute(string group, double order = double.MaxValue)
			: base(typeof(TagOptionDefinitionProvider)) {
			if (group == null)
				throw new ArgumentNullException(nameof(group));
			Group = group;
			Order = order;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; }

		/// <summary>
		/// Group, eg. <see cref="PredefinedHexViewGroupNames.HexEditor"/>
		/// </summary>
		public string Group { get; }
	}
}
