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

using System;
using System.Text;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A read only assembly name
	/// </summary>
	public interface IDmdAssemblyName {
		/// <summary>
		/// Gets the simple name
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the version
		/// </summary>
		Version Version { get; }

		/// <summary>
		/// Gets the culture name
		/// </summary>
		string CultureName { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		DmdAssemblyNameFlags RawFlags { get; }

		/// <summary>
		/// Gets the flags. The content type and processor architecture bits are ignored, use <see cref="RawFlags"/> instead
		/// </summary>
		DmdAssemblyNameFlags Flags { get; }

		/// <summary>
		/// Gets the processor architecture
		/// </summary>
		DmdProcessorArchitecture ProcessorArchitecture { get; }

		/// <summary>
		/// Gets the content type
		/// </summary>
		DmdAssemblyContentType ContentType { get; }

		/// <summary>
		/// Gets the public key
		/// </summary>
		/// <returns></returns>
		byte[] GetPublicKey();

		/// <summary>
		/// Gets the public key token
		/// </summary>
		/// <returns></returns>
		byte[] GetPublicKeyToken();

		/// <summary>
		/// Gets the hash algorithm
		/// </summary>
		DmdAssemblyHashAlgorithm HashAlgorithm { get; }

		/// <summary>
		/// Gets the full assembly name
		/// </summary>
		string FullName { get; }

		/// <summary>
		/// Creates a read only assembly name
		/// </summary>
		/// <returns></returns>
		DmdReadOnlyAssemblyName AsReadOnly();
	}

	static class DmdAssemblyNameExtensions {
		public static void FormatFullNameTo(this IDmdAssemblyName self, StringBuilder sb) =>
			DmdAssemblyNameFormatter.Format(sb, self.Name, self.Version, self.CultureName, self.GetPublicKeyToken(), self.RawFlags, isPublicKeyToken: true);
	}
}
