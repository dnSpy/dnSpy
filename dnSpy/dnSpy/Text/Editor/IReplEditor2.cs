/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	interface IReplEditor2 : IReplEditor {
		int? OffsetOfPrompt { get; }

		/// <summary>
		/// true if the caret is within the command line(s). It can also return true if the caret
		/// is within the primary/secondary promts.
		/// </summary>
		bool IsAtEditingPosition { get; }

		/// <summary>
		/// Primary prompt, eg. "> "
		/// </summary>
		string PrimaryPrompt { get; }

		/// <summary>
		/// Secondary prompt, eg. ". "
		/// </summary>
		string SecondaryPrompt { get; }

		string SearchText { get; set; }
		int FilterOffset(int offset);

		void ClearInput();

		void SelectSameTextPreviousCommand();
		void SelectSameTextNextCommand();
		bool TrySubmit(bool force);
		ReplSubBufferInfo FindBuffer(int offset);
	}

	struct ReplSubBufferInfo {
		public ReplSubBuffer Buffer { get; }
		public int CodeBufferIndex { get; }
		public ReplSubBufferInfo(ReplSubBuffer buffer, int codeBufferIndex) {
			Buffer = buffer;
			CodeBufferIndex = codeBufferIndex;
		}
	}
}
