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

using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	/// <summary>
	/// IL frame IP address
	/// </summary>
	struct ILFrameIP {
		public uint Offset { get; }
		public CorDebugMappingResult Mapping { get; }

		/// <summary>
		/// The native code is in the prolog, so the value of the IP is 0.
		/// </summary>
		public bool IsProlog => (Mapping & CorDebugMappingResult.MAPPING_PROLOG) != 0;

		/// <summary>
		/// The native code is in an epilog, so the value of the IP is the address of the last instruction of the method.
		/// </summary>
		public bool IsEpilog => (Mapping & CorDebugMappingResult.MAPPING_EPILOG) != 0;

		/// <summary>
		/// No mapping information is available for the method, so the value of the IP is 0.
		/// </summary>
		public bool HasNoInfo => (Mapping & CorDebugMappingResult.MAPPING_NO_INFO) != 0;

		/// <summary>
		/// Although there is mapping information for the method, the current address cannot be mapped to Microsoft intermediate language (MSIL) code. The value of the IP is 0.
		/// </summary>
		public bool IsUnmappedAddress => (Mapping & CorDebugMappingResult.MAPPING_UNMAPPED_ADDRESS) != 0;

		/// <summary>
		/// Either the method maps exactly to MSIL code or the frame has been interpreted, so the value of the IP is accurate.
		/// </summary>
		public bool IsExact => (Mapping & CorDebugMappingResult.MAPPING_EXACT) != 0;

		/// <summary>
		/// The method was successfully mapped, but the value of the IP may be approximate.
		/// </summary>
		public bool IsApproximate => (Mapping & CorDebugMappingResult.MAPPING_APPROXIMATE) != 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <param name="mapping">Mapping flags</param>
		public ILFrameIP(uint offset, CorDebugMappingResult mapping) {
			Offset = offset;
			Mapping = mapping;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (IsExact)
				return string.Format("0x{0:X4}", Offset);
			if (IsApproximate)
				return string.Format("~0x{0:X4}", Offset);
			if (IsProlog)
				return "prolog";
			if (IsEpilog)
				return "epilog";
			return string.Format("(0x{0:X4}, {1})", Offset, Mapping);
		}
	}
}
