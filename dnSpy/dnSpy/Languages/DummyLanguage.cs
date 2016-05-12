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
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;

namespace dnSpy.Languages {
	sealed class DummyLanguage : Language {
		public override string FileExtension => ".---";
		public override Guid GenericGuid => new Guid("CAE0EC7B-4311-4C48-AF7C-36E5EA71249A");
		public override Guid ContentTypeGuid => new Guid(ContentTypes.PLAIN_TEXT);
		public override string GenericNameUI => "---";
		public override double OrderUI => double.MaxValue;
		public override Guid UniqueGuid => new Guid("E4E6F1AA-FF88-48BC-B44C-49585E66DCF0");
		public override string UniqueNameUI => "---";
		public override IDecompilerSettings Settings { get; }

		sealed class DummySettings : IDecompilerSettings {
			public IDecompilerSettings Clone() => new DummySettings();

			public IEnumerable<IDecompilerOption> Options {
				get { yield break; }
			}

			public override bool Equals(object obj) => obj is DummySettings;
			public override int GetHashCode() => 0;
		}

		public DummyLanguage() {
			this.Settings = new DummySettings();
		}
	}
}
