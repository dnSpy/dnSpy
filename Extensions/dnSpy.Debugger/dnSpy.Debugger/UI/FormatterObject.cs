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

namespace dnSpy.Debugger.UI {
	sealed class FormatterObject<TVM> where TVM : class {
		public TVM VM { get; }

		/// <summary>
		/// See <see cref="Contracts.Text.Classification.PredefinedTextClassifierTags"/>
		/// </summary>
		public string Tag { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="vm">VM</param>
		/// <param name="tag">See <see cref="Contracts.Text.Classification.PredefinedTextClassifierTags"/></param>
		public FormatterObject(TVM vm, string tag) {
			VM = vm ?? throw new ArgumentNullException(nameof(vm));
			Tag = tag ?? throw new ArgumentNullException(nameof(tag));
		}
	}
}
