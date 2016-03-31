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

using dnSpy.Contracts.Scripting.Roslyn;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace dnSpy.Scripting.Roslyn.Common {
	sealed class PrintOptionsImpl : IPrintOptions {
		public PrintOptions RoslynPrintOptions {
			get { return printOptions; }
		}
		readonly PrintOptions printOptions;

		public string Ellipsis {
			get { return printOptions.Ellipsis; }
			set { printOptions.Ellipsis = value; }
		}

		public bool EscapeNonPrintableCharacters {
			get { return printOptions.EscapeNonPrintableCharacters; }
			set { printOptions.EscapeNonPrintableCharacters = value; }
		}

		public int MaximumOutputLength {
			get { return printOptions.MaximumOutputLength; }
			set { printOptions.MaximumOutputLength = value; }
		}

		public Contracts.Scripting.Roslyn.MemberDisplayFormat MemberDisplayFormat {
			get { return (Contracts.Scripting.Roslyn.MemberDisplayFormat)printOptions.MemberDisplayFormat; }
			set { printOptions.MemberDisplayFormat = (Microsoft.CodeAnalysis.Scripting.Hosting.MemberDisplayFormat)value; }
		}

		public int NumberRadix {
			get { return printOptions.NumberRadix; }
			set { printOptions.NumberRadix = value; }
		}

		public PrintOptionsImpl() {
			this.printOptions = new PrintOptions();
		}
	}
}
