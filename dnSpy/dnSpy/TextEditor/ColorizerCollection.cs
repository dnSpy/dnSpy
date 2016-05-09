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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.TextEditor;

namespace dnSpy.TextEditor {
	sealed class ColorizerCollection : IDisposable {
		ITextSnapshotColorizer[] AutoColorizers { get; set; } = Array.Empty<ITextSnapshotColorizer>();
		List<ITextSnapshotColorizer> UserColorizers { get; } = new List<ITextSnapshotColorizer>();

		readonly DnSpyTextEditor owner;
		readonly ITextSnapshotColorizerCreator textBufferColorizerCreator;

		public ColorizerCollection(DnSpyTextEditor owner, ITextSnapshotColorizerCreator textBufferColorizerCreator) {
			Debug.Assert(owner.TextBuffer != null);
			this.owner = owner;
			this.textBufferColorizerCreator = textBufferColorizerCreator;
			RecreateAutoColorizers();
		}

		public void RecreateAutoColorizers() {
			ClearAutoColorizers();
			AutoColorizers = textBufferColorizerCreator.Create(owner.TextBuffer).ToArray();
			OnColorizersUpdated();
		}

		void OnColorizersUpdated() => owner.TextArea.TextView.Redraw();

		void ClearAutoColorizers() {
			foreach (var c in AutoColorizers)
				(c as IDisposable)?.Dispose();
			AutoColorizers = Array.Empty<ITextSnapshotColorizer>();
		}

		public void Add(ITextSnapshotColorizer colorizer) {
			if (colorizer == null)
				throw new ArgumentNullException(nameof(colorizer));
			UserColorizers.Add(colorizer);
			OnColorizersUpdated();
		}

		public bool Remove(ITextSnapshotColorizer colorizer) {
			if (colorizer == null)
				throw new ArgumentNullException(nameof(colorizer));
			bool wasRemoved = UserColorizers.Remove(colorizer);
			if (wasRemoved)
				OnColorizersUpdated();
			return wasRemoved;
		}

		public ITextSnapshotColorizer[] GetAllColorizers() {
			var list = new ITextSnapshotColorizer[UserColorizers.Count + AutoColorizers.Length];
			int i = 0;
			for (int j = 0; j < UserColorizers.Count;)
				list[i++] = UserColorizers[j++];
			for (int j = 0; j < AutoColorizers.Length;)
				list[i++] = AutoColorizers[j++];
			return list;
		}

		public void Dispose() {
			ClearAutoColorizers();
			foreach (var c in UserColorizers)
				(c as IDisposable)?.Dispose();
			UserColorizers.Clear();
		}
	}
}
