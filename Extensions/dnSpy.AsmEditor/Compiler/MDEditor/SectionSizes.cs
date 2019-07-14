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

// Copied from dnlib

using System;
using System.Collections.Generic;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	readonly struct SectionSizeInfo {
		/// <summary>
		/// Length of section
		/// </summary>
		public readonly uint length;

		/// <summary>
		/// Section characteristics
		/// </summary>
		public readonly uint characteristics;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Length of section</param>
		/// <param name="characteristics">Section characteristics</param>
		public SectionSizeInfo(uint length, uint characteristics) {
			this.length = length;
			this.characteristics = characteristics;
		}
	}

	/// <summary>
	/// Calculates the optional header section sizes
	/// </summary>
	readonly struct SectionSizes {
		public readonly uint SizeOfHeaders;
		public readonly uint SizeOfImage;
		public readonly uint BaseOfData, BaseOfCode;
		public readonly uint SizeOfCode, SizeOfInitdData, SizeOfUninitdData;

		public SectionSizes(uint fileAlignment, uint sectionAlignment, uint headerLen, Func<IEnumerable<SectionSizeInfo>> getSectionSizeInfos) {
			SizeOfHeaders = AlignUp(headerLen, fileAlignment);
			SizeOfImage = AlignUp(SizeOfHeaders, sectionAlignment);
			BaseOfData = 0;
			BaseOfCode = 0;
			SizeOfCode = 0;
			SizeOfInitdData = 0;
			SizeOfUninitdData = 0;
			foreach (var section in getSectionSizeInfos()) {
				uint sectAlignedVs = AlignUp(section.length, sectionAlignment);
				uint fileAlignedVs = AlignUp(section.length, fileAlignment);

				bool isCode = (section.characteristics & 0x20) != 0;
				bool isInitdData = (section.characteristics & 0x40) != 0;
				bool isUnInitdData = (section.characteristics & 0x80) != 0;

				if (BaseOfCode == 0 && isCode)
					BaseOfCode = SizeOfImage;
				if (BaseOfData == 0 && (isInitdData || isUnInitdData))
					BaseOfData = SizeOfImage;
				if (isCode)
					SizeOfCode += fileAlignedVs;
				if (isInitdData)
					SizeOfInitdData += fileAlignedVs;
				if (isUnInitdData)
					SizeOfUninitdData += fileAlignedVs;

				SizeOfImage += sectAlignedVs;
			}
		}

		static uint AlignUp(uint v, uint alignment) => (v + alignment - 1) & ~(alignment - 1);
	}
}
