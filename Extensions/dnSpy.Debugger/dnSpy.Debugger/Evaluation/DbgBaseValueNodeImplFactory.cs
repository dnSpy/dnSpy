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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	static class DbgBaseValueNodeImplFactory {
		public static DbgBaseValueNodeImpl Create(DbgLanguage language, DbgRuntime runtime, DbgBaseEngineValueNode baseEngineValueNode) {
			if (baseEngineValueNode == null)
				throw new ArgumentNullException(nameof(baseEngineValueNode));
			switch (baseEngineValueNode) {
			case DbgEngineValueNode valueNode:			return new DbgValueNodeImpl(language, runtime, valueNode);
			case DbgEngineErrorValueNode errorNode:		return new DbgErrorValueNodeImpl(language, runtime, errorNode);
			default:									throw new InvalidOperationException();
			}
		}
	}
}
