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
	/// Assembly name
	/// </summary>
	public sealed class DmdAssemblyName : IDmdAssemblyName {
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
		public DmdAssemblyNameFlags RawFlags { get; set; }

		/// <summary>
		/// Gets/sets the flags. The content type and processor architecture bits are ignored, use <see cref="RawFlags"/> instead
		/// </summary>
		public DmdAssemblyNameFlags Flags {
			get => RawFlags & ~(DmdAssemblyNameFlags.ContentType_Mask | DmdAssemblyNameFlags.PA_FullMask);
			set => RawFlags = (RawFlags & (DmdAssemblyNameFlags.ContentType_Mask | DmdAssemblyNameFlags.PA_FullMask)) | (value & ~(DmdAssemblyNameFlags.ContentType_Mask | DmdAssemblyNameFlags.PA_FullMask));
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public DmdProcessorArchitecture ProcessorArchitecture {
			get => (DmdProcessorArchitecture)((int)(RawFlags & DmdAssemblyNameFlags.PA_Mask) >> 4);
			set => RawFlags = (RawFlags & ~DmdAssemblyNameFlags.PA_FullMask) | ((DmdAssemblyNameFlags)((int)value << 4) & DmdAssemblyNameFlags.PA_Mask);
		}

		/// <summary>
		/// Gets/sets the content type
		/// </summary>
		public DmdAssemblyContentType ContentType {
			get => (DmdAssemblyContentType)((int)(RawFlags & DmdAssemblyNameFlags.ContentType_Mask) >> 9);
			set => RawFlags = (RawFlags & ~DmdAssemblyNameFlags.ContentType_Mask) | ((DmdAssemblyNameFlags)((int)value << 9) & DmdAssemblyNameFlags.ContentType_Mask);
		}

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
				RawFlags &= ~DmdAssemblyNameFlags.PublicKey;
			else
				RawFlags |= DmdAssemblyNameFlags.PublicKey;
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
		public string FullName => DmdAssemblyNameFormatter.Format(Name, Version, CultureName, GetPublicKeyToken(), RawFlags, isPublicKeyToken: true);

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
			RawFlags = DmdAssemblyNameFlags.None;
		}

		DmdAssemblyName(DmdAssemblyName other) {
			Name = other.Name;
			Version = other.Version;
			CultureName = other.CultureName;
			RawFlags = other.RawFlags;
			HashAlgorithm = other.HashAlgorithm;
			publicKey = CloneArray(other.publicKey);
			publicKeyToken = CloneArray(other.publicKeyToken);
		}

		internal static T[] CloneArray<T>(T[] array) {
			if (array == null)
				return null;
			var res = new T[array.Length];
			Array.Copy(array, res, res.Length);
			return res;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assemblyName">Assembly name</param>
		public DmdAssemblyName(string assemblyName) {
			if (assemblyName == null)
				throw new ArgumentNullException(nameof(assemblyName));
			Impl.DmdTypeNameParser.ParseAssemblyName(assemblyName, out string name, out Version version, out string cultureName, out DmdAssemblyNameFlags flags, out this.publicKey, out this.publicKeyToken, out DmdAssemblyHashAlgorithm hashAlgorithm);
			Name = name;
			Version = version;
			CultureName = cultureName;
			RawFlags = flags;
			HashAlgorithm = hashAlgorithm;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Assembly name</param>
		public DmdAssemblyName(IDmdAssemblyName name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			Name = name.Name;
			Version = name.Version;
			CultureName = name.CultureName;
			RawFlags = name.RawFlags;
			publicKey = CloneArray(name.GetPublicKey());
			publicKeyToken = CloneArray(name.GetPublicKeyToken());
			HashAlgorithm = name.HashAlgorithm;
		}

		/// <summary>
		/// Creates a read only assembly name
		/// </summary>
		/// <returns></returns>
		public DmdReadOnlyAssemblyName AsReadOnly() => new DmdReadOnlyAssemblyName(this);

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns></returns>
		public DmdAssemblyName Clone() => new DmdAssemblyName(this);
	}
}
