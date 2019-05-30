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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex.Operations;

namespace dnSpy.Hex.Editor.Search {
	abstract class HexSearchServiceFactory {
		public abstract bool IsSearchDataValid(HexDataKind dataKind, string searchString, bool matchCase, bool isBigEndian);
		public abstract HexSearchService? TryCreateHexSearchService(HexDataKind dataKind, string searchString, bool matchCase, bool isBigEndian);
	}

	[Export(typeof(HexSearchServiceFactory))]
	sealed class HexSearchServiceFactoryImpl : HexSearchServiceFactory {
		readonly HexSearchServiceProvider hexSearchServiceProvider;

		[ImportingConstructor]
		HexSearchServiceFactoryImpl(HexSearchServiceProvider hexSearchServiceProvider) => this.hexSearchServiceProvider = hexSearchServiceProvider;

		public override bool IsSearchDataValid(HexDataKind dataKind, string searchString, bool matchCase, bool isBigEndian) {
			if (searchString == string.Empty)
				return false;

			switch (dataKind) {
			case HexDataKind.Bytes:
				return hexSearchServiceProvider.IsValidByteSearchString(searchString);

			case HexDataKind.Utf8String:
			case HexDataKind.Utf16String:
				return true;

			case HexDataKind.Byte:
			case HexDataKind.SByte:
			case HexDataKind.Int16:
			case HexDataKind.UInt16:
			case HexDataKind.Int32:
			case HexDataKind.UInt32:
			case HexDataKind.Int64:
			case HexDataKind.UInt64:
			case HexDataKind.Single:
			case HexDataKind.Double:
				return !(DataParser.TryParseData(searchString, dataKind, isBigEndian) is null);

			default:
				throw new InvalidOperationException();
			}
		}

		public override HexSearchService? TryCreateHexSearchService(HexDataKind dataKind, string searchString, bool matchCase, bool isBigEndian) {
			if (searchString == string.Empty)
				return null;

			byte[]? data;
			switch (dataKind) {
			case HexDataKind.Bytes:
				if (!hexSearchServiceProvider.IsValidByteSearchString(searchString))
					return null;
				return hexSearchServiceProvider.CreateByteSearchService(searchString);

			case HexDataKind.Utf8String:
				return hexSearchServiceProvider.CreateUtf8StringSearchService(searchString, matchCase);

			case HexDataKind.Utf16String:
				return hexSearchServiceProvider.CreateUtf16StringSearchService(searchString, matchCase, isBigEndian);

			case HexDataKind.Byte:
			case HexDataKind.SByte:
			case HexDataKind.Int16:
			case HexDataKind.UInt16:
			case HexDataKind.Int32:
			case HexDataKind.UInt32:
			case HexDataKind.Int64:
			case HexDataKind.UInt64:
			case HexDataKind.Single:
			case HexDataKind.Double:
				data = DataParser.TryParseData(searchString, dataKind, isBigEndian);
				if (data is null)
					return null;
				return hexSearchServiceProvider.CreateByteSearchService(data);

			default:
				throw new InvalidOperationException();
			}
		}
	}
}
