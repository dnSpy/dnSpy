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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.Hex.PE {
	[Export(typeof(HexStructureInfoProviderFactory))]
	sealed class HexStructureInfoProviderFactoryImpl : HexStructureInfoProviderFactory {
		readonly PEStructureProviderFactory peStructureProviderFactory;

		[ImportingConstructor]
		HexStructureInfoProviderFactoryImpl(PEStructureProviderFactory peStructureProviderFactory) {
			this.peStructureProviderFactory = peStructureProviderFactory;
		}

		public override HexStructureInfoProvider Create(HexView hexView) {
			//TODO: The current code only supports file layout, not memory layout
			if (hexView.Buffer.IsMemory)
				return null;
			var pePosition = HexPosition.Zero;
			var peStructureProvider = peStructureProviderFactory.TryGetProvider(hexView.Buffer, pePosition);
			if (peStructureProvider == null)
				return null;
			return new HexStructureInfoProviderImpl(peStructureProvider);
		}
	}

	sealed class HexStructureInfoProviderImpl : HexStructureInfoProvider {
		readonly PEStructure peStructure;

		public HexStructureInfoProviderImpl(PEStructureProvider peStructureProvider) {
			if (peStructureProvider == null)
				throw new ArgumentNullException(nameof(peStructureProvider));
			peStructure = new PEStructure(peStructureProvider);
		}

		sealed class PEStructure {
			public HexBuffer Buffer => peStructureProvider.Buffer;
			readonly PEStructureProvider peStructureProvider;
			readonly HexVM[] hexStructures;
			readonly HexSpan metadataTablesSpan;
			readonly MethodBodyInfoProvider methodBodyInfoProvider;

			public PEStructure(PEStructureProvider peStructureProvider) {
				this.peStructureProvider = peStructureProvider;
				methodBodyInfoProvider = MethodBodyInfoProvider.TryCreate(peStructureProvider);

				var list = new List<HexVM> {
					peStructureProvider.ImageDosHeader,
					peStructureProvider.ImageFileHeader,
					peStructureProvider.ImageOptionalHeader,
				};
				if (peStructureProvider.ImageCor20Header != null)
					list.Add(peStructureProvider.ImageCor20Header);
				if (peStructureProvider.StorageSignature != null)
					list.Add(peStructureProvider.StorageSignature);
				if (peStructureProvider.StorageHeader != null)
					list.Add(peStructureProvider.StorageHeader);
				if (peStructureProvider.TablesStream != null)
					list.Add(peStructureProvider.TablesStream);
				list.AddRange(peStructureProvider.Sections);
				list.AddRange(peStructureProvider.StorageStreams);
				hexStructures = list.ToArray();

				var tblsStream = peStructureProvider.TablesStream;
				if (tblsStream != null) {
					var first = tblsStream.MetaDataTables.FirstOrDefault(a => a != null);
					var last = tblsStream.MetaDataTables.LastOrDefault(a => a != null);
					Debug.Assert(first != null);
					if (first != null)
						metadataTablesSpan = HexSpan.FromBounds(first.Span.Start, last.Span.End);
				}
			}

			public FieldAndStructure? GetField(HexPosition position) {
				foreach (var structure in hexStructures) {
					if (structure.Span.Contains(position)) {
						foreach (var field in structure.HexFields) {
							if (field.IsVisible && field.Span.Contains(position))
								return new FieldAndStructure(structure, field);
						}
					}
				}
				if (metadataTablesSpan.Contains(position)) {
					foreach (var mdTbl in peStructureProvider.TablesStream.MetaDataTables) {
						if (mdTbl == null || !mdTbl.Span.Contains(position))
							continue;
						var offset = position - mdTbl.Span.Start;
						if (offset >= uint.MaxValue)
							break;
						uint index = (uint)(offset.ToUInt64() / (uint)mdTbl.TableInfo.RowSize);
						Debug.Assert(index < mdTbl.Rows);
						if (index >= mdTbl.Rows)
							break;
						var record = mdTbl.Get((int)index);
						foreach (var field in record.HexFields) {
							if (field.IsVisible && field.Span.Contains(position))
								return new FieldAndStructure(record, field);
						}
						break;
					}
				}
				return null;
			}

			public MethodBodyInfoAndField? GetMethodBodyInfoAndField(HexPosition position) =>
				methodBodyInfoProvider.GetMethodBodyInfoAndField(position);
		}

		struct FieldAndStructure {
			public HexVM Structure { get; }
			public HexField Field { get; }
			public FieldAndStructure(HexVM structure, HexField field) {
				if (structure == null)
					throw new ArgumentNullException(nameof(structure));
				if (field == null)
					throw new ArgumentNullException(nameof(field));
				Structure = structure;
				Field = field;
			}
		}

		FieldAndStructure? GetField(HexPosition position) => peStructure.GetField(position);

		public override IEnumerable<HexStructureField> GetFields(HexPosition position) {
			var info = GetField(position);
			if (info != null) {
				var buffer = peStructure.Buffer;
				yield return new HexStructureField(new HexBufferSpan(buffer, info.Value.Structure.Span), HexStructureFieldKind.Structure);
				yield return new HexStructureField(new HexBufferSpan(buffer, info.Value.Field.Span), HexStructureFieldKind.CurrentField);
			}

			var mfInfo = peStructure.GetMethodBodyInfoAndField(position);
			if (mfInfo != null) {
				var minfo = mfInfo.Value.MethodBodyInfo;
				var buffer = peStructure.Buffer;
				yield return new HexStructureField(new HexBufferSpan(buffer, minfo.HeaderSpan), HexStructureFieldKind.SubStructure);
				if (minfo.InstructionsSpan.Length != 0)
					yield return new HexStructureField(new HexBufferSpan(buffer, minfo.InstructionsSpan), HexStructureFieldKind.SubStructure);
				if (minfo.ExceptionsSpan.Length != 0)
					yield return new HexStructureField(new HexBufferSpan(buffer, minfo.ExceptionsSpan), HexStructureFieldKind.SubStructure);
				yield return new HexStructureField(new HexBufferSpan(buffer, mfInfo.Value.FieldInfo.Span), HexStructureFieldKind.CurrentField);
			}
		}

		public override object GetReference(HexPosition position) {
			var info = GetField(position);
			if (info != null)
				return new HexFieldReference(peStructure.Buffer, info.Value.Field);

			var mfInfo = peStructure.GetMethodBodyInfoAndField(position);
			if (mfInfo != null) {
				var minfo = mfInfo.Value.MethodBodyInfo;
				if (minfo.InstructionsSpan.Contains(position))
					return new HexMethodReference(peStructure.Buffer, minfo.Token.Rid, (uint)(position - minfo.InstructionsSpan.Start).ToUInt64());
				return new HexMethodReference(peStructure.Buffer, minfo.Token.Rid, null);
			}

			return null;
		}

		public override object GetToolTip(HexPosition position) {
			var info = GetField(position);
			if (info != null)
				return CreateFieldToolTip(info.Value);

			var mfInfo = peStructure.GetMethodBodyInfoAndField(position);
			if (mfInfo != null)
				return CreateMethodBodyToolTip(mfInfo.Value.MethodBodyInfo, mfInfo.Value.FieldInfo, position);

			return null;
		}

		object CreateFieldToolTip(FieldAndStructure info) {
			var output = new StringBuilderTextColorOutput();
			output.Write(BoxedTextColor.ValueType, info.Structure.Name);
			output.Write(BoxedTextColor.Punctuation, ".");
			output.Write(BoxedTextColor.InstanceField, info.Field.Name);
			return output.ToString();
		}

		object CreateMethodBodyToolTip(MethodBodyInfo minfo, MethodBodyFieldInfo finfo, HexPosition position) {
			const string methodHeader = "MethodHeader";
			const string methodBody = "MethodBody";
			const string exceptionHeader = "ExceptionHeader";
			const string exceptionClause = "ExceptionClause";
			const string exceptionClauses = "ExceptionClauses";
			const string unknown = "???";
			var output = new StringBuilderTextColorOutput();

			output.Write(BoxedTextColor.Text, "Method");
			output.WriteSpace();
			output.Write(BoxedTextColor.Number, "0x" + minfo.Token.Raw.ToString("X8"));
			output.WriteLine();

			switch (finfo.FieldKind) {
			case MethodBodyFieldKind.None:
				return null;

			case MethodBodyFieldKind.Unknown:
				output.Write(BoxedTextColor.ValueType, methodBody);
				break;

			case MethodBodyFieldKind.LargeHeaderFlags:
				output.Write(BoxedTextColor.ValueType, methodHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "Flags");
				break;

			case MethodBodyFieldKind.LargeHeaderMaxStack:
				output.Write(BoxedTextColor.ValueType, methodHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "MaxStack");
				break;

			case MethodBodyFieldKind.SmallHeaderCodeSize:
			case MethodBodyFieldKind.LargeHeaderCodeSize:
				output.Write(BoxedTextColor.ValueType, methodHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "CodeSize");
				break;

			case MethodBodyFieldKind.LargeHeaderLocalVarSigTok:
				output.Write(BoxedTextColor.ValueType, methodHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "LocalVarSigTok");
				break;

			case MethodBodyFieldKind.LargeHeaderUnknown:
				output.Write(BoxedTextColor.ValueType, methodHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.Error, unknown);
				break;

			case MethodBodyFieldKind.InstructionBytes:
				//TODO: Disassemble the instruction
				output.Write(BoxedTextColor.ValueType, methodBody);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "Instructions");
				output.Write(BoxedTextColor.Punctuation, ":");
				output.WriteSpace();
				output.Write(BoxedTextColor.Text, "offset");
				output.WriteSpace();
				output.Write(BoxedTextColor.Number, (position - minfo.InstructionsSpan.Start).ToString());
				break;

			case MethodBodyFieldKind.SmallExceptionHeaderKind:
			case MethodBodyFieldKind.LargeExceptionHeaderKind:
				output.Write(BoxedTextColor.ValueType, exceptionHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "Kind");
				break;

			case MethodBodyFieldKind.SmallExceptionHeaderDataSize:
			case MethodBodyFieldKind.LargeExceptionHeaderDataSize:
				output.Write(BoxedTextColor.ValueType, exceptionHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "DataSize");
				break;

			case MethodBodyFieldKind.SmallExceptionHeaderPadding:
				output.Write(BoxedTextColor.ValueType, exceptionHeader);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "Padding");
				break;

			case MethodBodyFieldKind.SmallExceptionClauseFlags:
			case MethodBodyFieldKind.LargeExceptionClauseFlags:
				output.Write(BoxedTextColor.ValueType, exceptionClause);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "Flags");
				break;

			case MethodBodyFieldKind.SmallExceptionClauseTryOffset:
			case MethodBodyFieldKind.LargeExceptionClauseTryOffset:
				output.Write(BoxedTextColor.ValueType, exceptionClause);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "TryOffset");
				break;

			case MethodBodyFieldKind.SmallExceptionClauseTryLength:
			case MethodBodyFieldKind.LargeExceptionClauseTryLength:
				output.Write(BoxedTextColor.ValueType, exceptionClause);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "TryLength");
				break;

			case MethodBodyFieldKind.SmallExceptionClauseHandlerOffset:
			case MethodBodyFieldKind.LargeExceptionClauseHandlerOffset:
				output.Write(BoxedTextColor.ValueType, exceptionClause);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "HandlerOffset");
				break;

			case MethodBodyFieldKind.SmallExceptionClauseHandlerLength:
			case MethodBodyFieldKind.LargeExceptionClauseHandlerLength:
				output.Write(BoxedTextColor.ValueType, exceptionClause);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "HandlerLength");
				break;

			case MethodBodyFieldKind.SmallExceptionClauseClassToken:
			case MethodBodyFieldKind.LargeExceptionClauseClassToken:
				output.Write(BoxedTextColor.ValueType, exceptionClause);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "ClassToken");
				break;

			case MethodBodyFieldKind.SmallExceptionClauseFilterOffset:
			case MethodBodyFieldKind.LargeExceptionClauseFilterOffset:
				output.Write(BoxedTextColor.ValueType, exceptionClause);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "FilterOffset");
				break;

			case MethodBodyFieldKind.SmallExceptionClauseReserved:
			case MethodBodyFieldKind.LargeExceptionClauseReserved:
				output.Write(BoxedTextColor.ValueType, exceptionClause);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.InstanceField, "Reserved");
				break;

			case MethodBodyFieldKind.ExceptionClausesUnknown:
				output.Write(BoxedTextColor.ValueType, exceptionClauses);
				output.Write(BoxedTextColor.Punctuation, ".");
				output.Write(BoxedTextColor.Error, unknown);
				break;

			default:
				throw new InvalidOperationException();
			}

			return output.ToString();
		}
	}
}
