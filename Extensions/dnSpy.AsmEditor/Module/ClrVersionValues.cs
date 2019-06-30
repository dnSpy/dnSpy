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

using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dnSpy.AsmEditor.Module {
	sealed class ClrVersionValues {
		public static readonly ClrVersionValues CLR10 = new ClrVersionValues(ClrVersion.CLR10, 0x00020000, 0x0100, MDHeaderRuntimeVersion.MS_CLR_10, AssemblyRefUser.CreateMscorlibReferenceCLR10());
		public static readonly ClrVersionValues CLR11 = new ClrVersionValues(ClrVersion.CLR11, 0x00020000, 0x0100, MDHeaderRuntimeVersion.MS_CLR_11, AssemblyRefUser.CreateMscorlibReferenceCLR11());
		public static readonly ClrVersionValues CLR20 = new ClrVersionValues(ClrVersion.CLR20, 0x00020005, 0x0200, MDHeaderRuntimeVersion.MS_CLR_20, AssemblyRefUser.CreateMscorlibReferenceCLR20());
		public static readonly ClrVersionValues CLR40 = new ClrVersionValues(ClrVersion.CLR40, 0x00020005, 0x0200, MDHeaderRuntimeVersion.MS_CLR_40, AssemblyRefUser.CreateMscorlibReferenceCLR40());

		public static readonly ClrVersionValues[] AllVersions = new ClrVersionValues[] {
			CLR10, CLR11, CLR20, CLR40,
		};

		public static ClrVersionValues? GetValues(ClrVersion clrVersion) {
			switch (clrVersion) {
			case ClrVersion.CLR10: return CLR10;
			case ClrVersion.CLR11: return CLR11;
			case ClrVersion.CLR20: return CLR20;
			case ClrVersion.CLR40: return CLR40;
			default: return null;
			}
		}

		public static ClrVersionValues? Find(uint cor20HeaderRuntimeVersion, ushort tablesHeaderVersion, string? runtimeVersion) {
			foreach (var clrValues in AllVersions) {
				if (clrValues.Cor20HeaderRuntimeVersion == cor20HeaderRuntimeVersion &&
					clrValues.TablesHeaderVersion == tablesHeaderVersion &&
					clrValues.RuntimeVersion == runtimeVersion)
					return clrValues;
			}
			return null;
		}

		public readonly ClrVersion ClrVersion;
		public readonly uint Cor20HeaderRuntimeVersion;
		public readonly ushort TablesHeaderVersion;
		public readonly string RuntimeVersion;
		public readonly AssemblyRef CorLibRef;

		public ClrVersionValues(ClrVersion clrVersion, uint cor20HeaderRuntimeVersion, ushort tablesHeaderVersion, string runtimeVersion, AssemblyRef corLibRef) {
			ClrVersion = clrVersion;
			Cor20HeaderRuntimeVersion = cor20HeaderRuntimeVersion;
			TablesHeaderVersion = tablesHeaderVersion;
			RuntimeVersion = runtimeVersion;
			CorLibRef = corLibRef;
		}
	}
}
