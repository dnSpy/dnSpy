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
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.MEF {

	// All interfaces must be public or MEF will complain

	public interface IContentTypeMetadata {
		IEnumerable<string> ContentTypes { get; }
	}

	public interface ITextViewRoleMetadata {
		IEnumerable<string> TextViewRoles { get; }
	}

	public interface IContentTypeAndTextViewRoleMetadata : IContentTypeMetadata, ITextViewRoleMetadata {
	}

	public interface IEditorFormatMetadata {
		string Name { get; }

		[DefaultValue(false)]
		bool UserVisible { get; }
	}

	public interface IClassificationFormatMetadata : IEditorFormatMetadata, IOrderable {
		IEnumerable<string> ClassificationTypeNames { get; }
	}

	public interface INameAndReplacesMetadata {
		[DefaultValue(null)]
		string Name { get; }

		[DefaultValue(null)]
		IEnumerable<string> Replaces { get; }
	}

	public interface INamedContentTypeMetadata : IContentTypeMetadata, INameAndReplacesMetadata {
	}

	public interface IAdornmentLayersMetadata : IOrderable {
		[DefaultValue(false)]
		bool IsOverlayLayer { get; }

		[DefaultValue(LayerKind.Normal)]
		LayerKind LayerKind { get; }
	}

	public interface IOrderableContentTypeAndTextViewRoleMetadata : IContentTypeAndTextViewRoleMetadata, IOrderable {
	}

	public interface ITaggerMetadata : IContentTypeMetadata {
		IEnumerable<Type> TagTypes { get; }
	}

	public interface INamedTaggerMetadata : ITaggerMetadata, INamedContentTypeMetadata {
	}

	public interface IViewTaggerMetadata : INamedTaggerMetadata {
		[DefaultValue(null)]
		IEnumerable<string> TextViewRoles { get; }
	}

	public interface IClassificationTypeDefinitionMetadata {
		[DefaultValue(null)]
		IEnumerable<string> BaseDefinition { get; }

		string Name { get; }
	}

	public interface IContentTypeDefinitionMetadata {
		[DefaultValue(null)]
		IEnumerable<string> BaseDefinition { get; }

		string Name { get; }
	}

	public interface ITextEditorFormatDefinitionMetadata {
		[DefaultValue(null)]
		IEnumerable<string> BaseDefinition { get; }

		string Name { get; }
	}

	public interface IWpfTextViewMarginMetadata : IOrderableContentTypeAndTextViewRoleMetadata {
		string MarginContainer { get; }

		[DefaultValue(1.0)]
		double GridCellLength { get; }

		[DefaultValue(GridUnitType.Auto)]
		GridUnitType GridUnitType { get; }

		[DefaultValue(null)]
		string OptionName { get; }

		[DefaultValue(null)]
		IEnumerable<string> Replaces { get; }
	}

	public interface IDeferrableContentTypeAndTextViewRoleMetadata : IContentTypeAndTextViewRoleMetadata {
		[DefaultValue(null)]
		string OptionName { get; }
	}

	public interface IOrderableContentTypeMetadata : IContentTypeMetadata, IOrderable {
	}

	public interface IGlyphMarginMetadata {
		[DefaultValue(null)]
		IEnumerable<string> GlyphMargins { get; }
	}

	public interface IGlyphMouseProcessorProviderMetadata : IGlyphMarginMetadata, IOrderableContentTypeMetadata {
	}

	public interface IGlyphMetadata : ITaggerMetadata, IOrderable {
	}

	public interface IGlyphTextMarkerMouseProcessorProviderMetadata : IOrderable {
		[DefaultValue(null)]
		IEnumerable<string> TextViewRoles { get; }
	}

	public interface IMarginContextMenuHandlerProviderMetadata {
		[DefaultValue(null)]
		IEnumerable<string> TextViewRoles { get; }

		string MarginName { get; }
	}
}
