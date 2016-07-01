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
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// Provides <see cref="ILineTransformSource"/> objects
	/// </summary>
	public interface ILineTransformSourceProvider {
		/// <summary>
		/// Creates an <see cref="ILineTransformSource"/> for the specified text view
		/// </summary>
		/// <param name="textView">The <see cref="IWpfTextView"/> on which the <see cref="ILineTransformSource"/> will format</param>
		/// <returns></returns>
		ILineTransformSource Create(IWpfTextView textView);
	}

	/// <summary>Metadata</summary>
	public interface ILineTransformSourceProviderMetadata {
		/// <summary>See <see cref="ExportLineTransformSourceProviderAttribute.ContentTypes"/></summary>
		string[] ContentTypes { get; }
		/// <summary>See <see cref="ExportLineTransformSourceProviderAttribute.TextViewRoles"/></summary>
		string[] TextViewRoles { get; }
	}

	/// <summary>
	/// Exports an <see cref="ILineTransformSourceProvider"/>
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportLineTransformSourceProviderAttribute : ExportAttribute, ILineTransformSourceProviderMetadata {
		/// <summary>
		/// Content types
		/// </summary>
		public string[] ContentTypes { get; }

		/// <summary>
		/// Text view roles
		/// </summary>
		public string[] TextViewRoles { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="contentType">Content type, eg. <see cref="Text.ContentTypes.TEXT"/></param>
		/// <param name="textViewRole">Text view role, eg. <see cref="PredefinedTextViewRoles.Interactive"/></param>
		public ExportLineTransformSourceProviderAttribute(string contentType, string textViewRole) {
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			if (textViewRole == null)
				throw new ArgumentNullException(nameof(textViewRole));
			ContentTypes = new[] { contentType };
			TextViewRoles = new[] { textViewRole };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="contentTypes"></param>
		/// <param name="textViewRoles"></param>
		public ExportLineTransformSourceProviderAttribute(string[] contentTypes, string[] textViewRoles)
			: base(typeof(ILineTransformSourceProvider)) {
			if (contentTypes == null)
				throw new ArgumentNullException(nameof(contentTypes));
			if (textViewRoles == null)
				throw new ArgumentNullException(nameof(textViewRoles));
			ContentTypes = contentTypes;
			TextViewRoles = textViewRoles;
		}
	}
}
