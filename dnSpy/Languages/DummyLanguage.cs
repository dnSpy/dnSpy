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

namespace dnSpy.Languages {
	sealed class DummyLanguage : Language {
		public override string FileExtension {
			get { return ".xxx"; }
		}

		public override Guid GenericGuid {
			get { return new Guid("CAE0EC7B-4311-4C48-AF7C-36E5EA71249A"); }
		}

		public override string GenericNameUI {
			get { return "---"; }
		}

		public override double OrderUI {
			get { return double.MaxValue; }
		}

		public override Guid UniqueGuid {
			get { return new Guid("E4E6F1AA-FF88-48BC-B44C-49585E66DCF0"); }
		}

		public override string UniqueNameUI {
			get { return "---"; }
		}
	}
}
