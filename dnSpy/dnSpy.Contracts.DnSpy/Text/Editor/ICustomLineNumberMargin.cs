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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Custom line number margin. The <see cref="ITextView"/> must have the
	/// <see cref="PredefinedDsTextViewRoles.CustomLineNumberMargin"/> role and
	/// you must call <see cref="SetOwner(ITextView, ICustomLineNumberMarginOwner)"/>.
	/// Option <see cref="DefaultTextViewHostOptions.LineNumberMarginId"/> is used
	/// to show or hide it after creation.
	/// </summary>
	public static class CustomLineNumberMargin {
		static readonly object Key = new object();

		/// <summary>
		/// Gets the custom line number margin
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <returns></returns>
		static ICustomLineNumberMargin GetMargin(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			ICustomLineNumberMargin margin;
			if (!textView.Properties.TryGetProperty(Key, out margin))
				throw new InvalidOperationException("No custom line number margin was found");
			return margin;
		}

		/// <summary>
		/// Sets the owner and must only be called once
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="owner">Owner</param>
		public static void SetOwner(ITextView textView, ICustomLineNumberMarginOwner owner) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			GetMargin(textView).SetOwner(owner);
		}

		internal static void SetMargin(ITextView textView, ICustomLineNumberMargin margin) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (margin == null)
				throw new ArgumentNullException(nameof(margin));
			textView.Properties.AddProperty(Key, margin);
		}
	}

	/// <summary>
	/// Custom line number margin
	/// </summary>
	interface ICustomLineNumberMargin {
		/// <summary>
		/// Sets the owner and must only be called once
		/// </summary>
		/// <param name="owner">Owner</param>
		void SetOwner(ICustomLineNumberMarginOwner owner);
	}

	/// <summary>
	/// Custom line number margin owner
	/// </summary>
	public interface ICustomLineNumberMarginOwner {
		/// <summary>
		/// Gets maximum number of digits in a line number or null to use the default value
		/// </summary>
		/// <returns></returns>
		int? GetMaxLineNumberDigits();

		/// <summary>
		/// Gets the line number or null to not print any line number. You should normally return null if
		/// <paramref name="viewLine"/>'s <see cref="ITextViewLine.IsFirstTextViewLineForSnapshotLine"/> is false.
		/// </summary>
		/// <param name="viewLine">View line</param>
		/// <param name="snapshotLine">Snapshot line</param>
		/// <param name="state">State, initially null</param>
		/// <returns></returns>
		int? GetLineNumber(ITextViewLine viewLine, ITextSnapshotLine snapshotLine, ref object state);

		/// <summary>
		/// Gets <see cref="TextFormattingRunProperties"/> for the line number text
		/// </summary>
		/// <param name="viewLine">View line</param>
		/// <param name="snapshotLine">Snapshot line</param>
		/// <param name="lineNumber">Line number returned by <see cref="GetLineNumber(ITextViewLine, ITextSnapshotLine, ref object)"/></param>
		/// <param name="state">State, initialized by <see cref="GetLineNumber(ITextViewLine, ITextSnapshotLine, ref object)"/></param>
		/// <returns></returns>
		TextFormattingRunProperties GetLineNumberTextFormattingRunProperties(ITextViewLine viewLine, ITextSnapshotLine snapshotLine, int lineNumber, object state);

		/// <summary>
		/// Gets the default text formatting properties
		/// </summary>
		/// <returns></returns>
		TextFormattingRunProperties GetDefaultTextFormattingRunProperties();

		/// <summary>
		/// Gets called when text formatting properties have changed
		/// </summary>
		/// <param name="classificationFormatMap">Classification format map</param>
		void OnTextPropertiesChanged(IClassificationFormatMap classificationFormatMap);

		/// <summary>
		/// Called when the margin is visible
		/// </summary>
		void OnVisible();

		/// <summary>
		/// Called when the margin is hidden and when the margin gets disposed
		/// </summary>
		void OnInvisible();
	}
}
