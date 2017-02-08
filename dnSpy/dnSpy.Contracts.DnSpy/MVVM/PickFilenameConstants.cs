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

using dnSpy.Contracts.Properties;

namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// Pick filename constants
	/// </summary>
	public static class PickFilenameConstants {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public static readonly string ImagesFilter = $"{dnSpy_Contracts_DnSpy_Resources.Files_Images}|*.png;*.gif;*.bmp;*.dib;*.jpg;*.jpeg;*.jpe;*.jif;*.jfif;*.jfi;*.ico;*.cur|{dnSpy_Contracts_DnSpy_Resources.AllFiles} (*.*)|*.*";
		public static readonly string StrongNameKeyFilter = $"{dnSpy_Contracts_DnSpy_Resources.Files_StrongNameKeyFiles} (*.snk)|*.snk|{dnSpy_Contracts_DnSpy_Resources.AllFiles} (*.*)|*.*";
		public static readonly string AnyFilenameFilter = $"{dnSpy_Contracts_DnSpy_Resources.AllFiles} (*.*)|*.*";
		public static readonly string DotNetExecutableFilter = $"{dnSpy_Contracts_DnSpy_Resources.Files_DotNetExecutables} (*.exe)|*.exe|{dnSpy_Contracts_DnSpy_Resources.AllFiles} (*.*)|*.*";
		public static readonly string DotNetAssemblyOrModuleFilter = $"{dnSpy_Contracts_DnSpy_Resources.Files_DotNetExecutables} (*.exe, *.dll, *.netmodule, *.winmd)|*.exe;*.dll;*.netmodule;*.winmd|{dnSpy_Contracts_DnSpy_Resources.AllFiles} (*.*)|*.*";
		public static readonly string NetModuleFilter = $"{dnSpy_Contracts_DnSpy_Resources.Files_DotNetNetModules} (*.netmodule)|*.netmodule|{dnSpy_Contracts_DnSpy_Resources.AllFiles} (*.*)|*.*";
		public static readonly string ExecutableFilter = $"{dnSpy_Contracts_DnSpy_Resources.Files_Executables} (*.exe)|*.exe|{dnSpy_Contracts_DnSpy_Resources.AllFiles} (*.*)|*.*";
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
