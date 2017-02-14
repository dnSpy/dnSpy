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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.BackgroundImage {
	/// <summary>
	/// Defines background image options. Use <see cref="ExportBackgroundImageOptionDefinitionAttribute"/>
	/// to export an instance. See also <see cref="IBackgroundImageOptionDefinition2"/>
	/// </summary>
	public interface IBackgroundImageOptionDefinition {
		/// <summary>
		/// Unique settings id
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Name shown in the UI
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Order of this option when shown in the UI
		/// </summary>
		double UIOrder { get; }

		/// <summary>
		/// true if the user can change the settings
		/// </summary>
		bool UserVisible { get; }

		/// <summary>
		/// Gets the default settings or null if none
		/// </summary>
		/// <returns></returns>
		DefaultImageSettings GetDefaultImageSettings();

		/// <summary>
		/// Returns true if the text view should use this instance's background image settings
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <returns></returns>
		bool IsSupported(ITextView textView);
	}

	/// <summary>
	/// Defines background image options. Use <see cref="ExportBackgroundImageOptionDefinitionAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IBackgroundImageOptionDefinition2 : IBackgroundImageOptionDefinition {
		/// <summary>
		/// Returns true if the hex view should use this instance's background image settings
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <returns></returns>
		bool IsSupported(HexView hexView);
	}

	/// <summary>Metadata</summary>
	public interface IBackgroundImageOptionDefinitionMetadata {
		/// <summary>See <see cref="ExportBackgroundImageOptionDefinitionAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IBackgroundImageOptionDefinition"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportBackgroundImageOptionDefinitionAttribute : ExportAttribute, IBackgroundImageOptionDefinitionMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order of this instance, eg. <see cref="BackgroundImageOptionDefinitionConstants.AttrOrder_Default"/></param>
		public ExportBackgroundImageOptionDefinitionAttribute(double order)
			: base(typeof(IBackgroundImageOptionDefinition)) => Order = order;

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; }
	}
}
