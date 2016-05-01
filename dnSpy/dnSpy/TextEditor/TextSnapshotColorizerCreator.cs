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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.TextEditor;

namespace dnSpy.TextEditor {
	/// <summary>
	/// Creates <see cref="ITextSnapshotColorizer"/> instances
	/// </summary>
	interface ITextSnapshotColorizerCreator {
		/// <summary>
		/// Creates new <see cref="ITextSnapshotColorizer"/> instances
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <returns></returns>
		IEnumerable<ITextSnapshotColorizer> Create(ITextBuffer textBuffer);
	}

	[Export, Export(typeof(ITextSnapshotColorizerCreator)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class TextSnapshotColorizerCreator : ITextSnapshotColorizerCreator {
		readonly ITextSnapshotColorizerProvider[] providers;

		[ImportingConstructor]
		TextSnapshotColorizerCreator([ImportMany] IEnumerable<ITextSnapshotColorizerProvider> providers) {
			this.providers = providers.ToArray();
		}

		public IEnumerable<ITextSnapshotColorizer> Create(ITextBuffer textBuffer) =>
			providers.SelectMany(a => a.Create(textBuffer));
	}
}
