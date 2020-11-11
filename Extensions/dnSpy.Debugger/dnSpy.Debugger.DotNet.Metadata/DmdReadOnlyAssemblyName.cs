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
using System.IO;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A read only assembly name
	/// </summary>
	public sealed class DmdReadOnlyAssemblyName : IDmdAssemblyName {
		/// <summary>
		/// Gets the simple name
		/// </summary>
		public string? Name { get; }

		/// <summary>
		/// Gets the version
		/// </summary>
		public Version? Version { get; }

		/// <summary>
		/// Gets the culture name
		/// </summary>
		public string? CultureName { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public DmdAssemblyNameFlags RawFlags { get; }

		/// <summary>
		/// Gets the flags. The content type and processor architecture bits are ignored, use <see cref="RawFlags"/> instead
		/// </summary>
		public DmdAssemblyNameFlags Flags => RawFlags & ~(DmdAssemblyNameFlags.ContentType_Mask | DmdAssemblyNameFlags.PA_FullMask);

		/// <summary>
		/// Gets the processor architecture
		/// </summary>
		public DmdProcessorArchitecture ProcessorArchitecture => (DmdProcessorArchitecture)((int)(RawFlags & DmdAssemblyNameFlags.PA_Mask) >> 4);

		/// <summary>
		/// Gets the content type
		/// </summary>
		public DmdAssemblyContentType ContentType => (DmdAssemblyContentType)((int)(RawFlags & DmdAssemblyNameFlags.ContentType_Mask) >> 9);

		/// <summary>
		/// Gets the public key
		/// </summary>
		/// <returns></returns>
		public byte[]? GetPublicKey() => publicKey;
		readonly byte[]? publicKey;

		/// <summary>
		/// Gets the public key token
		/// </summary>
		/// <returns></returns>
		public byte[]? GetPublicKeyToken() {
			if (publicKeyToken is null && publicKey is not null) {
				try {
					publicKeyToken = AssemblyHasher.CreatePublicKeyToken(publicKey);
				}
				catch (IOException) { }
			}
			return publicKeyToken;
		}
		byte[]? publicKeyToken;

		/// <summary>
		/// Gets the hash algorithm
		/// </summary>
		public DmdAssemblyHashAlgorithm HashAlgorithm { get; }

		/// <summary>
		/// Gets the full assembly name
		/// </summary>
		public string FullName => DmdAssemblyNameFormatter.Format(Name, Version, CultureName, GetPublicKeyToken(), RawFlags, isPublicKeyToken: true);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assemblyName">Assembly name</param>
		public DmdReadOnlyAssemblyName(string assemblyName) {
			if (assemblyName is null)
				throw new ArgumentNullException(nameof(assemblyName));
			Impl.DmdTypeNameParser.ParseAssemblyName(assemblyName, out var name, out var version, out var cultureName, out var flags, out publicKey, out publicKeyToken, out var hashAlgorithm);
			Name = name;
			Version = version;
			CultureName = cultureName;
			RawFlags = flags;
			HashAlgorithm = hashAlgorithm;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="cultureName">Culture or null</param>
		/// <param name="flags">Flags</param>
		/// <param name="publicKey">Public key or null</param>
		/// <param name="publicKeyToken">Public key token or null</param>
		/// <param name="hashAlgorithm">Hash algorithm</param>
		public DmdReadOnlyAssemblyName(string? name, Version? version, string? cultureName, DmdAssemblyNameFlags flags, byte[]? publicKey, byte[]? publicKeyToken, DmdAssemblyHashAlgorithm hashAlgorithm) {
			Name = name;
			Version = version;
			CultureName = cultureName;
			RawFlags = flags;
			this.publicKey = publicKey;
			this.publicKeyToken = publicKeyToken;
			HashAlgorithm = hashAlgorithm;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="cultureName">Culture or null</param>
		/// <param name="flags">Flags</param>
		/// <param name="publicKeyOrToken">Public key or public key token or null</param>
		/// <param name="hashAlgorithm">Hash algorithm</param>
		public DmdReadOnlyAssemblyName(string? name, Version? version, string? cultureName, DmdAssemblyNameFlags flags, byte[]? publicKeyOrToken, DmdAssemblyHashAlgorithm hashAlgorithm) {
			Name = name;
			Version = version;
			CultureName = cultureName;
			RawFlags = flags;
			if ((flags & DmdAssemblyNameFlags.PublicKey) != 0)
				publicKey = publicKeyOrToken;
			else
				publicKeyToken = publicKeyOrToken;
			HashAlgorithm = hashAlgorithm;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Assembly name</param>
		public DmdReadOnlyAssemblyName(IDmdAssemblyName name) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			Name = name.Name;
			Version = name.Version;
			CultureName = name.CultureName;
			RawFlags = name.RawFlags;
			publicKey = DmdAssemblyName.CloneArray(name.GetPublicKey());
			publicKeyToken = DmdAssemblyName.CloneArray(name.GetPublicKeyToken());
			HashAlgorithm = name.HashAlgorithm;
		}

		/// <summary>
		/// Converts it to a mutable assembly name
		/// </summary>
		/// <returns></returns>
		public DmdAssemblyName AsMutable() => new DmdAssemblyName(this);

		/// <summary>
		/// Creates a read only assembly name
		/// </summary>
		/// <returns></returns>
		public DmdReadOnlyAssemblyName AsReadOnly() => this;

		/// <summary>
		/// Gets the full assembly name
		/// </summary>
		/// <returns></returns>
		public override string ToString() => FullName;
	}
}
