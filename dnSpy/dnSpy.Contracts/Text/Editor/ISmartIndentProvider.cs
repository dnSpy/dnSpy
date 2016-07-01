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
	/// Smart indent provider. Use <see cref="ExportSmartIndentProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface ISmartIndentProvider {
		/// <summary>
		/// Creates a <see cref="ISmartIndent"/> for the given <see cref="ITextView"/> or returns null
		/// </summary>
		/// <param name="textView">The <see cref="ITextView"/> on which the <see cref="ISmartIndent"/> will navigate</param>
		/// <returns></returns>
		ISmartIndent CreateSmartIndent(ITextView textView);
	}

	/// <summary>Metadata</summary>
	public interface ISmartIndentProviderMetadata {
		/// <summary>See <see cref="ExportSmartIndentProviderAttribute.ContentTypes"/></summary>
		string[] ContentTypes { get; }
	}

	/// <summary>
	/// Exports an <see cref="ISmartIndentProvider"/>
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportSmartIndentProviderAttribute : ExportAttribute, ISmartIndentProviderMetadata {
		/// <summary>
		/// Content types
		/// </summary>
		public string[] ContentTypes { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="contentTypes">Content types, eg. <see cref="Text.ContentTypes.TEXT"/></param>
		public ExportSmartIndentProviderAttribute(params string[] contentTypes)
			: base(typeof(ISmartIndentProvider)) {
			if (contentTypes == null)
				throw new ArgumentNullException(nameof(contentTypes));
			ContentTypes = contentTypes;
		}
	}
}
