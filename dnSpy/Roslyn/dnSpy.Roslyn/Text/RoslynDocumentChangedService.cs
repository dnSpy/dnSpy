/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.Text {
	interface IRoslynDocumentChangedService {
		event EventHandler<RoslynDocumentChangedEventArgs>? DocumentChanged;
		void RaiseDocumentChanged(ITextSnapshot snapshot);
	}

	[Export(typeof(IRoslynDocumentChangedService))]
	sealed class RoslynDocumentChangedService : IRoslynDocumentChangedService {
		public event EventHandler<RoslynDocumentChangedEventArgs>? DocumentChanged;

		public void RaiseDocumentChanged(ITextSnapshot snapshot) {
			if (snapshot is null)
				throw new ArgumentNullException(nameof(snapshot));
			DocumentChanged?.Invoke(this, new RoslynDocumentChangedEventArgs(snapshot));
		}
	}

	sealed class RoslynDocumentChangedEventArgs : EventArgs {
		public ITextSnapshot Snapshot { get; }
		public RoslynDocumentChangedEventArgs(ITextSnapshot snapshot) => Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
	}
}
