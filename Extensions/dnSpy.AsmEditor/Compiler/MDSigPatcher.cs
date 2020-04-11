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
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dnSpy.AsmEditor.Compiler {
	unsafe struct MDSigPatcher {
		readonly List<byte> sigBuilder;
		readonly RemappedTypeTokens remappedTypeTokens;
		readonly byte* startPos;
		readonly byte* endPos;
		byte* currPos;
		bool usingBuilder;
		int recursionCounter;
		const int MAX_RECURSION = 100;

		sealed class InvalidSignatureException : Exception { }

		MDSigPatcher(List<byte> sigBuilder, RemappedTypeTokens remappedTypeTokens, RawModuleBytes moduleData, uint blobOffset, uint sigOffset) {
			if ((ulong)blobOffset + sigOffset > (ulong)moduleData.Size)
				ThrowInvalidSignatureException();
			this.sigBuilder = sigBuilder;
			this.remappedTypeTokens = remappedTypeTokens;
			currPos = (byte*)moduleData.Pointer + blobOffset + sigOffset;
			uint size = MDPatcherUtils.ReadCompressedUInt32(ref currPos, (byte*)moduleData.Pointer + moduleData.Size);
			startPos = currPos;
			endPos = currPos + size;
			if ((ulong)(endPos - (byte*)moduleData.Pointer) > (ulong)moduleData.Size)
				ThrowInvalidSignatureException();
			usingBuilder = false;
			recursionCounter = 0;
		}

		static void ThrowInvalidSignatureException() => throw new InvalidSignatureException();

		void SwitchToBuilder(uint bytesToRemove) {
			if (usingBuilder) {
				sigBuilder.RemoveRange(sigBuilder.Count - (int)bytesToRemove, (int)bytesToRemove);
				return;
			}
			sigBuilder.Clear();
			var end = currPos - bytesToRemove;
			for (var pos = startPos; pos < end; pos++)
				sigBuilder.Add(*pos);
			usingBuilder = true;
		}

		byte ReadByte() {
			if (currPos >= endPos)
				ThrowInvalidSignatureException();
			byte b = *currPos++;
			if (usingBuilder)
				sigBuilder.Add(b);
			return b;
		}

		uint ReadCompressedUInt32() {
			byte b = ReadByte();
			if ((b & 0x80) == 0)
				return b;

			if ((b & 0xC0) == 0x80)
				return (uint)(((b & 0x3F) << 8) | ReadByte());
			return (uint)(((b & 0x1F) << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
		}

		int ReadCompressedInt32() {
			byte b = ReadByte();
			if ((b & 0x80) == 0) {
				if ((b & 1) != 0)
					return -0x40 | (b >> 1);
				return b >> 1;
			}

			uint tmp;
			if ((b & 0xC0) == 0x80) {
				tmp = (uint)(((b & 0x3F) << 8) | ReadByte());
				if ((tmp & 1) != 0)
					return -0x2000 | (int)(tmp >> 1);
				return (int)(tmp >> 1);
			}

			tmp = (uint)(((b & 0x1F) << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
			if ((tmp & 1) != 0)
				return -0x10000000 | (int)(tmp >> 1);
			return (int)(tmp >> 1);
		}

		void WriteCompressedUInt32(uint value) {
			if (usingBuilder) {
				if (value <= 0x7F)
					sigBuilder.Add((byte)value);
				else if (value <= 0x3FFF) {
					sigBuilder.Add((byte)((value >> 8) | 0x80));
					sigBuilder.Add((byte)value);
				}
				else if (value <= 0x1FFFFFFF) {
					sigBuilder.Add((byte)((value >> 24) | 0xC0));
					sigBuilder.Add((byte)(value >> 16));
					sigBuilder.Add((byte)(value >> 8));
					sigBuilder.Add((byte)value);
				}
				else
					ThrowInvalidSignatureException();
			}
			else {
				if (value <= 0x7F)
					*currPos++ = (byte)value;
				else if (value <= 0x3FFF) {
					*currPos++ = (byte)((value >> 8) | 0x80);
					*currPos++ = (byte)value;
				}
				else if (value <= 0x1FFFFFFF) {
					*currPos++ = (byte)((value >> 24) | 0xC0);
					*currPos++ = (byte)(value >> 16);
					*currPos++ = (byte)(value >> 8);
					*currPos++ = (byte)value;
				}
				else
					ThrowInvalidSignatureException();
			}
		}

		byte[]? GetResult() {
			Debug.Assert(currPos == endPos, "We didn't read the full signature or it has garbage bytes");
			if (!usingBuilder)
				return null;
			return sigBuilder.ToArray();
		}

		public static byte[]? PatchTypeSignature(List<byte> sigBuilder, RemappedTypeTokens remappedTypeTokens, RawModuleBytes moduleData, uint blobOffset, uint sigOffset) {
			try {
				var patcher = new MDSigPatcher(sigBuilder, remappedTypeTokens, moduleData, blobOffset, sigOffset);
				patcher.PatchTypeSignature();
				return patcher.GetResult();
			}
			catch (MDPatcherUtils.InvalidMetadataException) {
			}
			catch (InvalidSignatureException) {
			}
			Debug.Fail("Failed to patch type sig");
			return null;
		}

		public static byte[]? PatchCallingConventionSignature(List<byte> sigBuilder, RemappedTypeTokens remappedTypeTokens, RawModuleBytes moduleData, uint blobOffset, uint sigOffset) {
			try {
				var patcher = new MDSigPatcher(sigBuilder, remappedTypeTokens, moduleData, blobOffset, sigOffset);
				patcher.PatchCallingConventionSignature();
				return patcher.GetResult();
			}
			catch (MDPatcherUtils.InvalidMetadataException) {
			}
			catch (InvalidSignatureException) {
			}
			Debug.Fail("Failed to patch calling convention sig");
			return null;
		}

		/// <summary>
		/// Reads a type sig. Returns true if it was a Sentinel
		/// </summary>
		bool PatchTypeSignature() {
			if (recursionCounter > MAX_RECURSION)
				return false;
			try {
				recursionCounter++;
				uint num;
				for (;;) {
					switch ((ElementType)ReadByte()) {
					case ElementType.Void:
					case ElementType.Boolean:
					case ElementType.Char:
					case ElementType.I1:
					case ElementType.U1:
					case ElementType.I2:
					case ElementType.U2:
					case ElementType.I4:
					case ElementType.U4:
					case ElementType.I8:
					case ElementType.U8:
					case ElementType.R4:
					case ElementType.R8:
					case ElementType.String:
					case ElementType.TypedByRef:
					case ElementType.I:
					case ElementType.U:
					case ElementType.Object:
						return false;

					case ElementType.Sentinel:
						return true;

					case ElementType.Ptr:
					case ElementType.ByRef:
					case ElementType.SZArray:
					case ElementType.Pinned:
						break;

					case ElementType.ValueType:
					case ElementType.Class:
						ReadTypeDefOrRef();
						return false;

					case ElementType.FnPtr:
						PatchCallingConventionSignature();
						return false;

					case ElementType.CModReqd:
					case ElementType.CModOpt:
						ReadTypeDefOrRef();
						break;

					case ElementType.Var:
					case ElementType.MVar:
						ReadCompressedUInt32();
						return false;

					case ElementType.ValueArray:
						PatchTypeSignature();
						ReadCompressedUInt32();
						return false;

					case ElementType.Module:
						ReadCompressedUInt32();
						return false;

					case ElementType.GenericInst:
						PatchTypeSignature();
						num = ReadCompressedUInt32();
						for (uint i = 0; i < num; i++)
							PatchTypeSignature();
						return false;

					case ElementType.Array:
						PatchTypeSignature();
						var rank = ReadCompressedUInt32();
						if (rank == 0)
							return false;
						num = ReadCompressedUInt32();
						var sizes = new List<uint>((int)num);
						for (uint i = 0; i < num; i++)
							ReadCompressedUInt32();
						num = ReadCompressedUInt32();
						for (uint i = 0; i < num; i++)
							ReadCompressedInt32();
						return false;

					case ElementType.Internal:
						for (num = 0; num < (uint)IntPtr.Size; num++)
							ReadByte();
						return false;

					case ElementType.End:
					case ElementType.R:
					default:
						return false;
					}
				}
			}
			finally {
				recursionCounter--;
			}
		}

		void ReadTypeDefOrRef() {
			var start = currPos;
			uint codedToken = ReadCompressedUInt32();
			uint compressedLen = (uint)(currPos - start);

			if (!CodedToken.TypeDefOrRef.Decode(codedToken, out MDToken token))
				ThrowInvalidSignatureException();
			if (!remappedTypeTokens.TryGetValue(token.Raw, out uint newToken))
				return;
			uint newCodedToken = CodedToken.TypeDefOrRef.Encode(newToken);
			uint newCodedTokenLen = (uint)MDPatcherUtils.GetCompressedUInt32Length(newCodedToken);
			if (usingBuilder || newCodedTokenLen != compressedLen) {
				SwitchToBuilder(compressedLen);
				WriteCompressedUInt32(newCodedToken);
			}
			else {
				currPos = start;
				WriteCompressedUInt32(newCodedToken);
			}
		}

		void PatchCallingConventionSignature() {
			if (recursionCounter > MAX_RECURSION)
				return;
			try {
				recursionCounter++;

				uint count, i;
				var callingConvention = (CallingConvention)ReadByte();
				switch (callingConvention & CallingConvention.Mask) {
				case CallingConvention.Default:
				case CallingConvention.C:
				case CallingConvention.StdCall:
				case CallingConvention.ThisCall:
				case CallingConvention.FastCall:
				case CallingConvention.VarArg:
				case CallingConvention.NativeVarArg:
				case CallingConvention.Property:
					if ((callingConvention & CallingConvention.Generic) != 0)
						ReadCompressedUInt32();
					count = ReadCompressedUInt32();
					PatchTypeSignature();
					for (i = 0; i < count; i++) {
						if (PatchTypeSignature())
							i--;
					}
					break;

				case CallingConvention.Field:
					PatchTypeSignature();
					break;

				case CallingConvention.LocalSig:
				case CallingConvention.GenericInst:
					count = ReadCompressedUInt32();
					for (i = 0; i < count; i++)
						PatchTypeSignature();
					break;

				case CallingConvention.Unmanaged:
				default:
					break;
				}
			}
			finally {
				recursionCounter--;
			}
		}
	}
}
