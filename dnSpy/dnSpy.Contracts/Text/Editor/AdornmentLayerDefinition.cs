/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Defines an adornment layer. Use <see cref="ExportAdornmentLayerDefinitionAttribute"/>
	/// to export an instance.
	/// </summary>
	public sealed class AdornmentLayerDefinition {
	}

	/// <summary>Metadata</summary>
	public interface IAdornmentLayerDefinitionMetadata {
		/// <summary>See <see cref="ExportAdornmentLayerDefinitionAttribute.DisplayName"/></summary>
		string DisplayName { get; }
		/// <summary>See <see cref="ExportAdornmentLayerDefinitionAttribute.Guid"/></summary>
		string Guid { get; }
		/// <summary>See <see cref="ExportAdornmentLayerDefinitionAttribute.Order"/></summary>
		double Order { get; }
		/// <summary>See <see cref="ExportAdornmentLayerDefinitionAttribute.IsOverlayLayer"/></summary>
		bool IsOverlayLayer { get; }
	}

	/// <summary>
	/// Exports an <see cref="AdornmentLayerDefinition"/>
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class ExportAdornmentLayerDefinitionAttribute : ExportAttribute, IAdornmentLayerDefinitionMetadata {
		/// <summary>
		/// Display name
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Layer guid, eg. <see cref="PredefinedAdornmentLayers.Selection"/>
		/// </summary>
		public string Guid { get; }

		/// <summary>
		/// Order, eg. <see cref="AdornmentLayerOrder.Text"/>
		/// </summary>
		public double Order { get; }

		/// <summary>
		/// true if it's an overlay layer
		/// </summary>
		public bool IsOverlayLayer { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="displayName">Display name</param>
		/// <param name="guid">Layer guid, eg. <see cref="PredefinedAdornmentLayers.Selection"/></param>
		/// <param name="order">Order, eg. <see cref="AdornmentLayerOrder.Text"/></param>
		/// <param name="isOverlayLayer">true if it's an overlay layer</param>
		public ExportAdornmentLayerDefinitionAttribute(string displayName, string guid, double order, bool isOverlayLayer = false)
			: base(typeof(AdornmentLayerDefinition)) {
			if (displayName == null)
				throw new ArgumentNullException(nameof(displayName));
			if (guid == null)
				throw new ArgumentNullException(nameof(guid));
			DisplayName = displayName;
			Guid = guid;
			Order = order;
			IsOverlayLayer = isOverlayLayer;
		}
	}
}
