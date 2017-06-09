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

using System;
using System.IO;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Assembly name
	/// </summary>
	public sealed class DmdAssemblyName {
		/// <summary>
		/// Gets/sets the simple name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets/sets the version
		/// </summary>
		public Version Version { get; set; }

		/// <summary>
		/// Gets/sets the culture name
		/// </summary>
		public string CultureName { get; set; }

		/// <summary>
		/// Gets/sets the flags
		/// </summary>
		public DmdAssemblyNameFlags Flags { get; set; }

		/// <summary>
		/// Gets the processor architecture
		/// </summary>
		public DmdProcessorArchitecture ProcessorArchitecture => (DmdProcessorArchitecture)((int)(Flags & DmdAssemblyNameFlags.PA_Mask) >> 4);

		/// <summary>
		/// Gets the content type
		/// </summary>
		public DmdAssemblyContentType ContentType => (DmdAssemblyContentType)((int)(Flags & DmdAssemblyNameFlags.ContentType_Mask) >> 9);

		/// <summary>
		/// Gets the public key
		/// </summary>
		/// <returns></returns>
		public byte[] GetPublicKey() => publicKey;
		byte[] publicKey;

		/// <summary>
		/// Sets the public key
		/// </summary>
		/// <param name="publicKey">Public key or null</param>
		public void SetPublicKey(byte[] publicKey) {
			this.publicKey = publicKey;
			if (publicKey == null)
				Flags &= ~DmdAssemblyNameFlags.PublicKey;
			else
				Flags |= DmdAssemblyNameFlags.PublicKey;
		}

		/// <summary>
		/// Gets the public key token
		/// </summary>
		/// <returns></returns>
		public byte[] GetPublicKeyToken() {
			if (publicKeyToken == null && publicKey != null) {
				try {
					publicKeyToken = AssemblyHasher.CreatePublicKeyToken(publicKey);
				}
				catch (IOException) { }
			}
			return publicKeyToken;
		}
		byte[] publicKeyToken;

		/// <summary>
		/// Sets the public key token
		/// </summary>
		/// <param name="publicKeyToken">Public key token</param>
		public void SetPublicKeyToken(byte[] publicKeyToken) => this.publicKeyToken = publicKeyToken;

		/// <summary>
		/// Gets/sets the hash algorithm
		/// </summary>
		public DmdAssemblyHashAlgorithm HashAlgorithm { get; set; }

		/// <summary>
		/// Gets the full assembly name
		/// </summary>
		public string FullName => DmdAssemblyNameFormatter.Format(Name, Version, CultureName, GetPublicKeyToken(), Flags, isPublicKeyToken: true);

		/// <summary>
		/// Gets the full assembly name
		/// </summary>
		/// <returns></returns>
		public override string ToString() => FullName;

		/// <summary>
		/// Constructor
		/// </summary>
		public DmdAssemblyName() {
			HashAlgorithm = DmdAssemblyHashAlgorithm.None;
			Flags = DmdAssemblyNameFlags.None;
		}

		DmdAssemblyName(DmdAssemblyName other) {
			Name = other.Name;
			Version = other.Version;
			CultureName = other.CultureName;
			Flags = other.Flags;
			HashAlgorithm = other.HashAlgorithm;
			publicKey = other.publicKey.CloneArray();
			publicKeyToken = other.publicKeyToken.CloneArray();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assemblyName">Assembly name</param>
		public DmdAssemblyName(string assemblyName) {
			if (assemblyName == null)
				throw new ArgumentNullException(nameof(assemblyName));
			Impl.DmdTypeNameParser.ParseAssemblyName(this, assemblyName);
		}

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns></returns>
		public DmdAssemblyName Clone() => new DmdAssemblyName(this);
	}
}
