// Copied from Roslyn and ported to C#

using System.Globalization;

namespace dnSpy.Roslyn.Debugger.ValueNodes.VisualBasic {
	static class SyntaxFacts {
		static readonly bool[] s_isIDChar = new bool[128] {
			false, false, false, false, false, false, false, false, false, false,
			false, false, false, false, false, false, false, false, false, false,
			false, false, false, false, false, false, false, false, false, false,
			false, false, false, false, false, false, false, false, false, false,
			false, false, false, false, false, false, false, false, true, true,
			true, true, true, true, true, true, true, true, false, false,
			false, false, false, false, false, true, true, true, true, true,
			true, true, true, true, true, true, true, true, true, true,
			true, true, true, true, true, true, true, true, true, true,
			true, false, false, false, false, true, false, true, true, true,
			true, true, true, true, true, true, true, true, true, true,
			true, true, true, true, true, true, true, true, true, true,
			true, true, true, false, false, false, false, false
		};

		public static bool IsIdentifierPartCharacter(char c) {
			if (c < 128)
				return IsNarrowIdentifierCharacter((ushort)c);
			return IsWideIdentifierCharacter(c);
		}

		static bool IsNarrowIdentifierCharacter(ushort c) => s_isIDChar[c];

		static bool IsWideIdentifierCharacter(char c) {
			var CharacterProperties = CharUnicodeInfo.GetUnicodeCategory(c);
			return IsPropAlphaNumeric(CharacterProperties) ||
				IsPropLetterDigit(CharacterProperties) ||
				IsPropConnectorPunctuation(CharacterProperties) ||
				IsPropCombining(CharacterProperties) ||
				IsPropOtherFormat(CharacterProperties);
		}

		static bool IsPropAlphaNumeric(UnicodeCategory CharacterProperties) => CharacterProperties <= UnicodeCategory.DecimalDigitNumber;
		static bool IsPropLetterDigit(UnicodeCategory CharacterProperties) => CharacterProperties == UnicodeCategory.LetterNumber;
		static bool IsPropConnectorPunctuation(UnicodeCategory CharacterProperties) => CharacterProperties == UnicodeCategory.ConnectorPunctuation;
		static bool IsPropCombining(UnicodeCategory CharacterProperties) => CharacterProperties >= UnicodeCategory.NonSpacingMark && CharacterProperties <= UnicodeCategory.EnclosingMark;
		static bool IsPropOtherFormat(UnicodeCategory CharacterProperties) => CharacterProperties == UnicodeCategory.Format;
	}
}
