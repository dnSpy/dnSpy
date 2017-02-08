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

namespace dnSpy.Contracts.Hex.Operations {
	/// <summary>
	/// Creates <see cref="HexSearchService"/> instances that can search for bytes or strings
	/// </summary>
	public abstract class HexSearchServiceProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexSearchServiceProvider() { }

		/// <summary>
		/// The character that matches any nibble in a byte
		/// </summary>
		const char wildcardCharacter = '?';

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for <see cref="byte"/> values
		/// </summary>
		/// <param name="value">Value to search for</param>
		/// <returns></returns>
		public HexSearchService CreateByteSearchService(byte value) => CreateByteSearchService(new byte[1] { value });

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for <see cref="sbyte"/> values
		/// </summary>
		/// <param name="value">Value to search for</param>
		/// <returns></returns>
		public HexSearchService CreateByteSearchService(sbyte value) => CreateByteSearchService(new byte[1] { (byte)value });

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for <see cref="short"/> values
		/// </summary>
		/// <param name="value">Value to search for</param>
		/// <param name="isBigEndian">true if big-endian, false if little-endian</param>
		/// <returns></returns>
		public HexSearchService CreateByteSearchService(short value, bool isBigEndian = false) => CreateByteSearchService(GetBytes((ushort)value, isBigEndian));

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for <see cref="ushort"/> values
		/// </summary>
		/// <param name="value">Value to search for</param>
		/// <param name="isBigEndian">true if big-endian, false if little-endian</param>
		/// <returns></returns>
		public HexSearchService CreateByteSearchService(ushort value, bool isBigEndian = false) => CreateByteSearchService(GetBytes(value, isBigEndian));

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for <see cref="int"/> values
		/// </summary>
		/// <param name="value">Value to search for</param>
		/// <param name="isBigEndian">true if big-endian, false if little-endian</param>
		/// <returns></returns>
		public HexSearchService CreateByteSearchService(int value, bool isBigEndian = false) => CreateByteSearchService(GetBytes((uint)value, isBigEndian));

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for <see cref="uint"/> values
		/// </summary>
		/// <param name="value">Value to search for</param>
		/// <param name="isBigEndian">true if big-endian, false if little-endian</param>
		/// <returns></returns>
		public HexSearchService CreateByteSearchService(uint value, bool isBigEndian = false) => CreateByteSearchService(GetBytes(value, isBigEndian));

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for <see cref="long"/> values
		/// </summary>
		/// <param name="value">Value to search for</param>
		/// <param name="isBigEndian">true if big-endian, false if little-endian</param>
		/// <returns></returns>
		public HexSearchService CreateByteSearchService(long value, bool isBigEndian = false) => CreateByteSearchService(GetBytes((ulong)value, isBigEndian));

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for <see cref="ulong"/> values
		/// </summary>
		/// <param name="value">Value to search for</param>
		/// <param name="isBigEndian">true if big-endian, false if little-endian</param>
		/// <returns></returns>
		public HexSearchService CreateByteSearchService(ulong value, bool isBigEndian = false) => CreateByteSearchService(GetBytes(value, isBigEndian));

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for <see cref="float"/> values
		/// </summary>
		/// <param name="value">Value to search for</param>
		/// <param name="isBigEndian">true if big-endian, false if little-endian</param>
		/// <returns></returns>
		public HexSearchService CreateByteSearchService(float value, bool isBigEndian = false) => CreateByteSearchService(GetBytes(value, isBigEndian));

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for <see cref="double"/> values
		/// </summary>
		/// <param name="value">Value to search for</param>
		/// <param name="isBigEndian">true if big-endian, false if little-endian</param>
		/// <returns></returns>
		public HexSearchService CreateByteSearchService(double value, bool isBigEndian = false) => CreateByteSearchService(GetBytes(value, isBigEndian));

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for bytes. The wildcard character ? matches any nibble (upper or lower 4 bits) in a byte.
		/// Use <see cref="IsValidByteSearchString(string)"/> to validate the input before calling this method.
		/// </summary>
		/// <param name="pattern">Pattern. Supported characters: whitespace, hex digits and the wildcard character '?'</param>
		/// <returns></returns>
		public HexSearchService CreateByteSearchService(string pattern) {
			if (pattern == null)
				throw new ArgumentNullException(nameof(pattern));
			int byteLength = GetByteLength(pattern);
			if (byteLength <= 0)
				throw new ArgumentOutOfRangeException(nameof(pattern));

			var bytes = new byte[byteLength];
			var mask = new byte[byteLength];
			int bytesIndex = 0;
			for (int i = 0; i < pattern.Length;) {
				i = SkipWhitespace(pattern, i);
				if (i >= pattern.Length)
					break;
				int b = 0, m = 0;
				var c = pattern[i++];
				if (c != wildcardCharacter) {
					m |= 0xF0;
					var v = HexToBin(c);
					if (v < 0)
						throw new ArgumentOutOfRangeException(nameof(pattern));
					b |= v << 4;
				}

				i = SkipWhitespace(pattern, i);
				if (i < pattern.Length) {
					c = pattern[i++];
					if (c != wildcardCharacter) {
						m |= 0x0F;
						var v = HexToBin(c);
						if (v < 0)
							throw new ArgumentOutOfRangeException(nameof(pattern));
						b |= v;
					}
				}
				bytes[bytesIndex] = (byte)b;
				mask[bytesIndex] = (byte)m;
				bytesIndex++;
			}
			if (bytesIndex != bytes.Length)
				throw new InvalidOperationException();

			return CreateByteSearchService(bytes, mask);
		}

		static int SkipWhitespace(string pattern, int index) {
			while (index < pattern.Length) {
				if (!char.IsWhiteSpace(pattern[index]))
					break;
				index++;
			}
			return index;
		}

		static int GetByteLength(string pattern) {
			int nibbles = 0;
			foreach (var c in pattern) {
				if (char.IsWhiteSpace(c))
					continue;
				if (c != wildcardCharacter && HexToBin(c) < 0)
					return -1;
				nibbles++;
			}
			return (nibbles + 1) / 2;
		}

		static int HexToBin(char c) {
			if ('0' <= c && c <= '9')
				return c - '0';
			if ('a' <= c && c <= 'f')
				return c - 'a' + 10;
			if ('A' <= c && c <= 'F')
				return c - 'A' + 10;
			return -1;
		}

		/// <summary>
		/// Checks whether <paramref name="pattern"/> only contains valid characters and at least one valid character
		/// </summary>
		/// <param name="pattern">Pattern</param>
		/// <returns></returns>
		public bool IsValidByteSearchString(string pattern) {
			if (pattern == null)
				throw new ArgumentNullException(nameof(pattern));
			return GetByteLength(pattern) > 0;
		}

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for bytes
		/// </summary>
		/// <param name="pattern">Bytes to search for</param>
		/// <returns></returns>
		public HexSearchService CreateByteSearchService(byte[] pattern) {
			if (pattern == null)
				throw new ArgumentNullException(nameof(pattern));
			var mask = new byte[pattern.Length];
			for (int i = 0; i < mask.Length; i++)
				mask[i] = 0xFF;
			return CreateByteSearchService(pattern, mask);
		}

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for bytes
		/// </summary>
		/// <param name="pattern">Bytes to search for</param>
		/// <param name="mask">Mask used when comparing values. This array must have the same length as <paramref name="pattern"/></param>
		/// <returns></returns>
		public abstract HexSearchService CreateByteSearchService(byte[] pattern, byte[] mask);

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for UTF-8 strings
		/// </summary>
		/// <param name="pattern">Pattern to search for</param>
		/// <param name="isCaseSensitive">true if it's case sensitive, false if it's case insensitive</param>
		/// <returns></returns>
		public abstract HexSearchService CreateUtf8StringSearchService(string pattern, bool isCaseSensitive);

		/// <summary>
		/// Creates a <see cref="HexSearchService"/> that can search for UTF-16 strings
		/// </summary>
		/// <param name="pattern">Pattern to search for</param>
		/// <param name="isCaseSensitive">true if it's case sensitive, false if it's case insensitive</param>
		/// <param name="isBigEndian">true if big-endian, false if little-endian</param>
		/// <returns></returns>
		public abstract HexSearchService CreateUtf16StringSearchService(string pattern, bool isCaseSensitive, bool isBigEndian = false);

		static byte[] GetBytes(ushort value, bool isBigEndian) {
			if (isBigEndian) {
				return new byte[2] {
					(byte)(value >> 8),
					(byte)value,
				};
			}
			return BitConverter.GetBytes(value);
		}

		static byte[] GetBytes(uint value, bool isBigEndian) {
			if (isBigEndian) {
				return new byte[4] {
					(byte)(value >> 24),
					(byte)(value >> 16),
					(byte)(value >> 8),
					(byte)value,
				};
			}
			return BitConverter.GetBytes(value);
		}

		static byte[] GetBytes(ulong value, bool isBigEndian) {
			if (isBigEndian) {
				return new byte[8] {
					(byte)(value >> 56),
					(byte)(value >> 48),
					(byte)(value >> 40),
					(byte)(value >> 32),
					(byte)(value >> 24),
					(byte)(value >> 16),
					(byte)(value >> 8),
					(byte)value,
				};
			}
			return BitConverter.GetBytes(value);
		}

		static byte[] GetBytes(float value, bool isBigEndian) {
			var bytes = BitConverter.GetBytes(value);
			if (isBigEndian)
				return GetBytes(BitConverter.ToUInt32(bytes, 0), isBigEndian);
			return bytes;
		}

		static byte[] GetBytes(double value, bool isBigEndian) {
			var bytes = BitConverter.GetBytes(value);
			if (isBigEndian)
				return GetBytes(BitConverter.ToUInt64(bytes, 0), isBigEndian);
			return bytes;
		}
	}
}
