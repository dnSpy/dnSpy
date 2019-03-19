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

using dnSpy.Contracts.Debugger.Engine.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	sealed class NullDbgEngineObjectIdFactory : DbgEngineObjectIdFactory {
		public static readonly DbgEngineObjectIdFactory Instance = new NullDbgEngineObjectIdFactory();
		NullDbgEngineObjectIdFactory() { }
		public override bool CanCreateObjectId(DbgEngineValue value) => false;
		public override DbgEngineObjectId CreateObjectId(DbgEngineValue value, uint id) => null;
		public override bool Equals(DbgEngineObjectId objectId, DbgEngineValue value) => false;
		public override int GetHashCode(DbgEngineObjectId objectId) => 0;
		public override int GetHashCode(DbgEngineValue value) => 0;
	}
}
