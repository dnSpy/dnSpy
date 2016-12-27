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

using System;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.Hex.Files.DotNet {
	sealed class DotNetHeadersImpl : DotNetHeaders {
		public override PeHeaders PeHeaders { get; }
		public override DotNetCor20Data Cor20 { get; }
		public override DotNetMetadataHeaders MetadataHeaders { get; }
		public override VirtualArrayData<ByteData> StrongNameSignature { get; }
		public override DotNetMethodProvider MethodProvider { get; }

		public DotNetHeadersImpl(PeHeaders peHeaders, DotNetCor20Data cor20, DotNetMetadataHeaders metadataHeaders, VirtualArrayData<ByteData> strongNameSignature, DotNetMethodProvider methodProvider) {
			if (peHeaders == null)
				throw new ArgumentNullException(nameof(peHeaders));
			if (cor20 == null)
				throw new ArgumentNullException(nameof(cor20));
			PeHeaders = peHeaders;
			Cor20 = cor20;
			MetadataHeaders = metadataHeaders;
			StrongNameSignature = strongNameSignature;
			MethodProvider = methodProvider;
		}
	}
}
