/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using System.Reflection;

#pragma warning disable 0436
#if USE_STABLE_VERSION
[assembly: AssemblyVersion(DnSpyAssemblyConstants.STABLE_ASSEMBLY_VERSION)]
#else
[assembly: AssemblyVersion(DnSpyAssemblyConstants.NONSTABLE_ASSEMBLY_VERSION)]
#endif
[assembly: AssemblyFileVersion(DnSpyAssemblyConstants.ASSEMBLY_FILE_VERSION)]
[assembly: AssemblyInformationalVersion(DnSpyAssemblyConstants.ASSEMBLY_INFORMATIONAL_VERSION)]

static class DnSpyAssemblyConstants {
	// Don't change the stable version number unless there are breaking changes
	public const string STABLE_ASSEMBLY_VERSION						= "3.0.0.0";
	public const string NONSTABLE_ASSEMBLY_VERSION					= "3.0.0.0";
	public const string ASSEMBLY_INFORMATIONAL_VERSION				= "3.0.0.0-beta";
	public const string ASSEMBLY_FILE_VERSION						= NONSTABLE_ASSEMBLY_VERSION;
}
#pragma warning restore 0436
