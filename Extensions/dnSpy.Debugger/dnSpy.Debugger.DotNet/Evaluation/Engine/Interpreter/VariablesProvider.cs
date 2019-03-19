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

using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	abstract class VariablesProvider {
		public abstract void Initialize(DbgEvaluationInfo evalInfo, DmdMethodBase method, DmdMethodBody body);
		public abstract DbgDotNetValue GetValueAddress(int index, DmdType targetType);
		public abstract DbgDotNetValueResult GetVariable(int index);
		public abstract string SetVariable(int index, DmdType targetType, object value);
		public abstract bool CanDispose(DbgDotNetValue value);
		public abstract void Clear();
	}
}
