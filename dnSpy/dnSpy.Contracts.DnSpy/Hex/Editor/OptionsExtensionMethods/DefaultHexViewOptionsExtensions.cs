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
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods {
	/// <summary>
	/// <see cref="DefaultHexViewOptions"/> extension methods
	/// </summary>
	static class DefaultHexViewOptionsExtensions {
		/// <summary>
		/// Returns true if the offset column is shown
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool ShowOffsetColumn(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.ShowOffsetColumnId);
		}

		/// <summary>
		/// Returns true if the values column is shown
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool ShowValuesColumn(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.ShowValuesColumnId);
		}

		/// <summary>
		/// Returns true if the ASCII column is shown
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool ShowAsciiColumn(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.ShowAsciiColumnId);
		}

		/// <summary>
		/// Gets the start position
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static HexPosition GetStartPosition(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.StartPositionId);
		}

		/// <summary>
		/// Gets the end position
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static HexPosition GetEndPosition(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.EndPositionId);
		}

		/// <summary>
		/// Gets the base position
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static HexPosition GetBasePosition(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.BasePositionId);
		}

		/// <summary>
		/// Returns true if the positions are relative to the base
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool UseRelativePositions(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.UseRelativePositionsId);
		}

		/// <summary>
		/// Returns size of the offset in bits
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static int GetOffsetBitSize(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.OffsetBitSizeId);
		}

		/// <summary>
		/// Returns the values display format
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static HexValuesDisplayFormat GetValuesDisplayFormat(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.HexValuesDisplayFormatId);
		}

		/// <summary>
		/// Returns the offset format
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static HexOffsetFormat GetOffsetFormat(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.HexOffsetFormatId);
		}

		/// <summary>
		/// Returns true if values are displayed in lower case hex
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsValuesLowerCaseHexEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.ValuesLowerCaseHexId);
		}

		/// <summary>
		/// Returns true if the offset is displayed in lower case hex
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsOffsetLowerCaseHexEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.OffsetLowerCaseHexId);
		}

		/// <summary>
		/// Returns the number of bytes that should be displayed per line
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static int GetBytesPerLine(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.BytesPerLineId);
		}

		/// <summary>
		/// Returns the number of bytes in a group
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static int GetGroupSizeInBytes(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.GroupSizeInBytesId);
		}

		/// <summary>
		/// Returns true if text should be colorized
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsColorizationEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.EnableColorizationId);
		}

		/// <summary>
		/// Returns true if the hex view prohibits user input
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool DoesViewProhibitUserInput(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.ViewProhibitUserInputId);
		}

		/// <summary>
		/// Returns true if refresh-screen-on-change is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsRefreshScreenOnChangeEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.RefreshScreenOnChangeId);
		}

		/// <summary>
		/// Returns the number of milliseconds to wait before refreshing the screen after the document gets changed
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static int GetRefreshScreenOnChangeWaitMilliSeconds(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.RefreshScreenOnChangeWaitMilliSecondsId);
		}

		/// <summary>
		/// Returns true if extra vertical pixels should be removed from text lines
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsRemoveExtraTextLineVerticalPixelsEnabled(this VSTE.IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultHexViewOptions.RemoveExtraTextLineVerticalPixelsId);
		}
	}
}
