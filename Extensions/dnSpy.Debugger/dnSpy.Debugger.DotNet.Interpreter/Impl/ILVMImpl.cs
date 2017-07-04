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

using System;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Interpreter.Impl {
	sealed class ILVMImpl : ILVM {
		public override object Execute(IDmdEvaluationContext context, DmdMethodBase method, VariablesProvider variablesProvider, ExecuteOptions options) {
			if ((object)method == null)
				throw new ArgumentNullException(nameof(method));
			if (variablesProvider == null)
				throw new ArgumentNullException(nameof(variablesProvider));

			throw new NotImplementedException();//TODO:
		}
	}
}
