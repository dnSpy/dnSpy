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

namespace dnSpy.Contracts.Text.Tagging {
	/// <summary>
	/// Creates an <see cref="ITagger{T}"/> for a given buffer. Use
	/// <see cref="ExportViewTaggerProviderAttribute"/> to export an instance.
	/// </summary>
	public interface IViewTaggerProvider {
		/// <summary>
		/// Creates a tag provider for the specified view and buffer
		/// </summary>
		/// <typeparam name="T">The type of the tag</typeparam>
		/// <param name="textView">Text view</param>
		/// <param name="buffer">Buffer</param>
		/// <returns></returns>
		ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag;
	}

	/// <summary>Metadata</summary>
	public interface IViewTaggerProviderMetadata {
		/// <summary>See <see cref="ExportViewTaggerProviderAttribute.ContentTypes"/></summary>
		string[] ContentTypes { get; }
		/// <summary>See <see cref="ExportViewTaggerProviderAttribute.Types"/></summary>
		Type[] Types { get; }
		/// <summary>See <see cref="ExportViewTaggerProviderAttribute.Roles"/></summary>
		string[] Roles { get; }
	}

	/// <summary>
	/// Exports an <see cref="IViewTaggerProvider"/>
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportViewTaggerProviderAttribute : ExportAttribute, IViewTaggerProviderMetadata {
		/// <summary>
		/// Gets the supported content types
		/// </summary>
		public string[] ContentTypes { get; }

		/// <summary>
		/// Gets the supported tag types
		/// </summary>
		public Type[] Types { get; }

		/// <summary>
		/// Gets the supported text view roles or null
		/// </summary>
		public string[] Roles { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Supported tag type</param>
		/// <param name="contentType">Supported content type</param>
		public ExportViewTaggerProviderAttribute(Type type, string contentType)
			: this(new[] { type }, new[] { contentType }) {
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="types">Supported tag types</param>
		/// <param name="contentTypes">Supported content types</param>
		public ExportViewTaggerProviderAttribute(Type[] types, string[] contentTypes)
			: base(typeof(IViewTaggerProvider)) {
			if (types == null)
				throw new ArgumentNullException(nameof(types));
			if (contentTypes == null)
				throw new ArgumentNullException(nameof(contentTypes));
			ContentTypes = contentTypes;
			Types = types;
		}
	}
}
