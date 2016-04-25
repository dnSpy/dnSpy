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
using System.Globalization;
using dnSpy.Properties;

namespace dnSpy.Culture {
	enum LanguageType {
		SystemLanguage,
		CultureInfo,
	}

	sealed class LanguageInfo : IEquatable<LanguageInfo> {
		public LanguageType Type { get; private set; }

		public CultureInfo CultureInfo {
			get { return cultureInfo; }
		}
		CultureInfo cultureInfo;

		public string UIName {
			get { return ToString(); }
		}

		public static LanguageInfo CreateSystemLanguage() {
			return new LanguageInfo(LanguageType.SystemLanguage);
		}

		public static LanguageInfo Create(CultureInfo cultureInfo) {
			if (cultureInfo == null)
				throw new ArgumentNullException();
			return new LanguageInfo(cultureInfo);
		}

		LanguageInfo(LanguageType type) {
			this.Type = type;
			this.cultureInfo = null;
		}

		LanguageInfo(CultureInfo cultureInfo) {
			this.Type = LanguageType.CultureInfo;
			this.cultureInfo = cultureInfo;
		}

		public bool Equals(LanguageInfo other) {
			if (Type != other.Type)
				return false;
			return Type != LanguageType.CultureInfo || cultureInfo.Equals(other.cultureInfo);
		}

		public override bool Equals(object obj) {
			return Equals(obj as LanguageInfo);
		}

		public override int GetHashCode() {
			switch (Type) {
			case LanguageType.SystemLanguage: return 0x69DCD8A8;
			case LanguageType.CultureInfo: return cultureInfo.GetHashCode();
			default: throw new InvalidOperationException();
			}
		}

		public override string ToString() {
			switch (Type) {
			case LanguageType.SystemLanguage: return dnSpy_Resources.Language_OperatingSystemLanguage;
			case LanguageType.CultureInfo: return cultureInfo.NativeName;
			default: throw new InvalidOperationException();
			}
		}
	}
}
