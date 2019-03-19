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

using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	readonly struct ILFrameIP {
		public uint Offset { get; }
		public CorDebugMappingResult Mapping { get; }

		public bool IsProlog => (Mapping & CorDebugMappingResult.MAPPING_PROLOG) != 0;
		public bool IsEpilog => (Mapping & CorDebugMappingResult.MAPPING_EPILOG) != 0;
		public bool HasNoInfo => (Mapping & CorDebugMappingResult.MAPPING_NO_INFO) != 0;
		public bool IsUnmappedAddress => (Mapping & CorDebugMappingResult.MAPPING_UNMAPPED_ADDRESS) != 0;
		public bool IsExact => (Mapping & CorDebugMappingResult.MAPPING_EXACT) != 0;
		public bool IsApproximate => (Mapping & CorDebugMappingResult.MAPPING_APPROXIMATE) != 0;

		public ILFrameIP(uint offset, CorDebugMappingResult mapping) {
			Offset = offset;
			Mapping = mapping;
		}

		public override string ToString() {
			if (IsExact)
				return $"0x{Offset:X4}";
			if (IsApproximate)
				return $"~0x{Offset:X4}";
			if (IsProlog)
				return "prolog";
			if (IsEpilog)
				return "epilog";
			return $"(0x{Offset:X4}, {Mapping})";
		}
	}
}
