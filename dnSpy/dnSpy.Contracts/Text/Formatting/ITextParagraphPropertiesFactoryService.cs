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
using System.Windows.Media.TextFormatting;

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// Creates <see cref="TextParagraphProperties"/> classes to be used when lines on the view are being formatted.
	/// Use <see cref="ExportTextParagraphPropertiesFactoryServiceAttribute"/> to export an instance.
	/// </summary>
	public interface ITextParagraphPropertiesFactoryService {
		/// <summary>
		/// Creates a <see cref="TextParagraphProperties"/> for the provided configuration or returns null
		/// </summary>
		/// <param name="formattedLineSource">The <see cref="IFormattedLineSource"/> that is performing the formatting of the line. You can access useful properties about the ongoing formatting operation from this object</param>
		/// <param name="textProperties">The <see cref="TextFormattingRunProperties"/> of the line for which the <see cref="TextParagraphProperties"/> are provided. This parameter can be used to obtain formatting information about the textual contents of the line</param>
		/// <param name="line">The <see cref="IMappingSpan"/> corresponding to the line that is being formatted or rendered</param>
		/// <param name="lineStart">The <see cref="IMappingPoint"/> corresponding to the beginning of the line segment that is being formatted. This parameter is used in word-wrap scenarios where a single <see cref="ITextSnapshotLine"/> results in multiple formatted or rendered lines on the view</param>
		/// <param name="lineSegment">The segment number of the line segment that has been currently formatted. This is a zero-based index and is applicable to word-wrapped lines. If a line is word-wrapped into 4 segments, you will receive 4 calls for the line with line segments of 0, 1, 2, and 3</param>
		/// <returns></returns>
		TextParagraphProperties Create(IFormattedLineSource formattedLineSource, TextFormattingRunProperties textProperties, IMappingSpan line, IMappingPoint lineStart, int lineSegment);
	}

	/// <summary>Metadata</summary>
	public interface ITextParagraphPropertiesFactoryServiceMetadata {
		/// <summary>See <see cref="ExportTextParagraphPropertiesFactoryServiceAttribute.ContentTypes"/></summary>
		string[] ContentTypes { get; }
	}

	/// <summary>
	/// Exports an <see cref="ITextParagraphPropertiesFactoryService"/>
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportTextParagraphPropertiesFactoryServiceAttribute : ExportAttribute, ITextParagraphPropertiesFactoryServiceMetadata {
		/// <summary>
		/// Content types
		/// </summary>
		public string[] ContentTypes { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="contentTypes">Content types, eg. <see cref="Text.ContentTypes.TEXT"/></param>
		public ExportTextParagraphPropertiesFactoryServiceAttribute(params string[] contentTypes)
			: base(typeof(ITextParagraphPropertiesFactoryService)) {
			if (contentTypes == null)
				throw new ArgumentNullException(nameof(contentTypes));
			ContentTypes = contentTypes;
		}
	}
}
