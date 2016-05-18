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

using System.Diagnostics;
using dnSpy.Contracts.AsmEditor.Compile;
using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Shared.Compile {
	static class Extensions {
		public static Platform ToPlatform(this TargetPlatform platform) {
			switch (platform) {
			case TargetPlatform.AnyCpu:					return Platform.AnyCpu;
			case TargetPlatform.X86:					return Platform.X86;
			case TargetPlatform.X64:					return Platform.X64;
			case TargetPlatform.Itanium:				return Platform.Itanium;
			case TargetPlatform.AnyCpu32BitPreferred:	return Platform.AnyCpu32BitPreferred;
			case TargetPlatform.Arm:					return Platform.Arm;
			default:
				Debug.Fail($"Unknown platform: {platform}");
				return Platform.AnyCpu;
			}
		}

		public static MetadataReference CreateMetadataReference(this CompilerMetadataReference mdRef) =>
			MetadataReference.CreateFromImage(mdRef.Data, mdRef.IsAssemblyReference ? MetadataReferenceProperties.Assembly : MetadataReferenceProperties.Module);
	}
}
