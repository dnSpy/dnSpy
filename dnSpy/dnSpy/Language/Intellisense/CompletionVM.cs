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
using System.ComponentModel;
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Language.Intellisense {
	sealed class CompletionVM : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		public object ImageObject => this;
		public object DisplayTextObject => this;
		public object SuffixObject => this;
		public Completion Completion { get; }

		public IEnumerable<CompletionIconVM> AttributeIcons => attributeIcons ?? (attributeIcons = CreateAttributeIcons());
		IEnumerable<CompletionIconVM> attributeIcons;

		public CompletionVM(Completion completion) {
			if (completion == null)
				throw new ArgumentNullException(nameof(completion));
			Completion = completion;
			Completion.Properties.AddProperty(typeof(CompletionVM), this);
		}

		public static CompletionVM TryGet(Completion completion) {
			if (completion == null)
				return null;
			CompletionVM vm;
			if (completion.Properties.TryGetProperty(typeof(CompletionVM), out vm))
				return vm;
			return null;
		}

		IEnumerable<CompletionIconVM> CreateAttributeIcons() {
			var icons = (Completion as Completion2)?.AttributeIcons;
			if (icons == null)
				return Array.Empty<CompletionIconVM>();
			var list = new List<CompletionIconVM>();
			foreach (var icon in icons)
				list.Add(new CompletionIconVM(icon));
			return list;
		}

		public void RefreshImages() {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageObject)));
			if (attributeIcons != null) {
				foreach (var vm in attributeIcons)
					vm.RefreshImages();
			}
		}
	}
}
