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

namespace dnSpy.Contracts.Text.Tagging {
	/// <summary>
	/// Creates an <see cref="ITagger{T}"/> for a given buffer. Use
	/// <see cref="ExportTaggerProviderAttribute"/> to export an instance.
	/// </summary>
	public interface ITaggerProvider {
		/// <summary>
		/// Creates a tag provider for the specified buffer
		/// </summary>
		/// <typeparam name="T">The type of the tag</typeparam>
		/// <param name="buffer">The buffer</param>
		/// <returns></returns>
		ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag;
	}

	/// <summary>Metadata</summary>
	public interface ITaggerProviderMetadata {
		/// <summary>See <see cref="ExportTaggerProviderAttribute.ContentTypes"/></summary>
		string[] ContentTypes { get; }
		/// <summary>See <see cref="ExportTaggerProviderAttribute.Types"/></summary>
		Type[] Types { get; }
	}

	/// <summary>
	/// Exports an <see cref="ITaggerProvider"/>
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportTaggerProviderAttribute : ExportAttribute, ITaggerProviderMetadata {
		/// <summary>
		/// Gets the supported content types
		/// </summary>
		public string[] ContentTypes { get; }

		/// <summary>
		/// Gets the supported tag types
		/// </summary>
		public Type[] Types { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Supported tag type</param>
		/// <param name="contentType">Supported content type</param>
		public ExportTaggerProviderAttribute(Type type, string contentType)
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
		public ExportTaggerProviderAttribute(Type[] types, string[] contentTypes)
			: base(typeof(ITaggerProvider)) {
			if (types == null)
				throw new ArgumentNullException(nameof(types));
			if (contentTypes == null)
				throw new ArgumentNullException(nameof(contentTypes));
			ContentTypes = contentTypes;
			Types = types;
		}
	}
}
