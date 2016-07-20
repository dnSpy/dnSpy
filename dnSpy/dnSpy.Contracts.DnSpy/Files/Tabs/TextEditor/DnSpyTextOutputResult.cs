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
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Contracts.Files.Tabs.TextEditor {
	/// <summary>
	/// dnSpy text result shown in the decompiler text editor
	/// </summary>
	public sealed class DnSpyTextOutputResult {
		/// <summary>
		/// Gets the text
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Gets the colors
		/// </summary>
		public CachedTextTokenColors CachedTextTokenColors { get; }

		/// <summary>
		/// Gets the references
		/// </summary>
		public SpanDataCollection<ReferenceInfo> ReferenceCollection { get; }

		/// <summary>
		/// Gets the IL code mappings
		/// </summary>
		public MemberMapping[] MemberMappings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="cachedTextTokenColors">Colors</param>
		/// <param name="referenceCollection">References</param>
		/// <param name="memberMappings">Debug info</param>
		public DnSpyTextOutputResult(string text, CachedTextTokenColors cachedTextTokenColors, SpanDataCollection<ReferenceInfo> referenceCollection, MemberMapping[] memberMappings) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (cachedTextTokenColors == null)
				throw new ArgumentNullException(nameof(cachedTextTokenColors));
			if (referenceCollection == null)
				throw new ArgumentNullException(nameof(referenceCollection));
			if (memberMappings == null)
				throw new ArgumentNullException(nameof(memberMappings));
			cachedTextTokenColors.Finish();
			Text = text;
			CachedTextTokenColors = cachedTextTokenColors;
			ReferenceCollection = referenceCollection;
			MemberMappings = memberMappings;
		}
	}
}
