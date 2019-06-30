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

// CLR error codes: https://github.com/dotnet/coreclr/blob/master/src/inc/corerror.xml

namespace dndbg.Engine {
	static class CordbgErrors {
		public const int CORDBG_E_PROCESS_TERMINATED = unchecked((int)0x80131301);
		public const int CORDBG_E_OBJECT_NEUTERED = unchecked((int)0x8013134F);
		public const int CORDBG_E_STATIC_VAR_NOT_AVAILABLE = unchecked((int)0x8013131A);
		public const int CORDBG_E_CLASS_NOT_LOADED = unchecked((int)0x80131303);
		public const int CORDBG_E_ILLEGAL_IN_NATIVE_CODE = unchecked((int)0x80131C25);
		public const int CORDBG_E_ILLEGAL_AT_GC_UNSAFE_POINT = unchecked((int)0x80131C23);
		public const int CORDBG_E_ILLEGAL_IN_OPTIMIZED_CODE = unchecked((int)0x80131C26);
		public const int CORDBG_E_ILLEGAL_IN_PROLOG = unchecked((int)0x80131C24);
		public const int CORDBG_E_UNCOMPATIBLE_PLATFORMS = unchecked((int)0x80131C30);
		public const int CORDBG_E_UNRECOVERABLE_ERROR = unchecked((int)0x80131300);
		public const int CORDBG_E_PROCESS_NOT_SYNCHRONIZED = unchecked((int)0x80131302);
		public const int CLDB_E_RECORD_NOTFOUND = unchecked((int)0x80131130);
		public const int CLDB_E_INDEX_NOTFOUND = unchecked((int)0x80131124);
		public const int CORDBG_E_IL_VAR_NOT_AVAILABLE = unchecked((int)0x80131304);
		public const int CORDBG_E_INVALID_OPCODE = unchecked((int)0x80131C4D);
		public const int CORDBG_E_UNSUPPORTED = unchecked((int)0x80131C4E);
		public const int META_E_BAD_SIGNATURE = unchecked((int)0x80131192);

		public static bool IsCantEvaluateError(int hr) => hr == CORDBG_E_ILLEGAL_IN_NATIVE_CODE ||
					hr == CORDBG_E_ILLEGAL_AT_GC_UNSAFE_POINT ||
					hr == CORDBG_E_ILLEGAL_IN_OPTIMIZED_CODE ||
					hr == CORDBG_E_ILLEGAL_IN_PROLOG;
	}
}
