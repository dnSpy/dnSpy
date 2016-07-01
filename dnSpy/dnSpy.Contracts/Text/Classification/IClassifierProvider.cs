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

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Creates <see cref="IClassifier"/> instances. Use
	/// <see cref="ExportClassifierProviderAttribute"/> to export an instance.
	/// </summary>
	public interface IClassifierProvider {
		/// <summary>
		/// Returns a <see cref="IClassifier"/> or null
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <returns></returns>
		IClassifier GetClassifier(ITextBuffer textBuffer);
	}

	/// <summary>Metadata</summary>
	public interface IClassifierProviderMetadata {
		/// <summary>See <see cref="ExportClassifierProviderAttribute.ContentTypes"/></summary>
		string[] ContentTypes { get; }
	}

	/// <summary>
	/// Exports an <see cref="IClassifierProvider"/>
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportClassifierProviderAttribute : ExportAttribute, IClassifierProviderMetadata {
		/// <summary>
		/// Content types
		/// </summary>
		public string[] ContentTypes { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="contentTypes">Content types, eg. <see cref="Text.ContentTypes.TEXT"/></param>
		public ExportClassifierProviderAttribute(params string[] contentTypes)
			: base(typeof(IClassifierProvider)) {
			if (contentTypes == null)
				throw new ArgumentNullException(nameof(contentTypes));
			ContentTypes = contentTypes;
		}
	}
}
