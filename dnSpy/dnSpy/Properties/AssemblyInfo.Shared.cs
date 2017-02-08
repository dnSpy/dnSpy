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

using System.Reflection;

#pragma warning disable 0436
[assembly: AssemblyVersion(DnSpyAssemblyConstants.ASSEMBLY_VERSION)]
[assembly: AssemblyFileVersion(DnSpyAssemblyConstants.ASSEMBLY_FILE_VERSION)]
[assembly: AssemblyInformationalVersion(DnSpyAssemblyConstants.ASSEMBLY_INFORMATIONAL_VERSION)]

static class DnSpyAssemblyConstants {
	// Update App.config whenever this value changes.
	public const string ASSEMBLY_VERSION							= "3.0.1.0";
	// This is shown in the title bar. 3 numbers are enough
	public const string ASSEMBLY_INFORMATIONAL_VERSION				= "3.x.x";
	public const string ASSEMBLY_FILE_VERSION						= ASSEMBLY_VERSION;
}
#pragma warning restore 0436
