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

using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Debugger.DotNet.Mono.CallStack;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed partial class DbgEngineImpl {
		public override DbgEngineStackWalker CreateStackWalker(DbgThread thread) {
			return new DbgEngineStackWalkerImpl();
		}

		public override void Freeze(DbgThread thread) {
			//TODO:
		}

		public override void Thaw(DbgThread thread) {
			//TODO:
		}

		public override void SetIP(DbgThread thread, DbgCodeLocation location) {
			//TODO:
		}

		public override bool CanSetIP(DbgThread thread, DbgCodeLocation location) {
			return false;//TODO:
		}
	}
}
