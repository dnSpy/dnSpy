/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.MVVM;

namespace dnSpy.BamlDecompiler {
	internal class BamlDisassembler {
		#region Record handler map

		static Action<BamlContext, BamlRecord> Thunk<TRecord>(Action<BamlContext, TRecord> handler) where TRecord : BamlRecord {
			return (ctx, record) => handler(ctx, (TRecord)record);
		}

		Dictionary<BamlRecordType, Action<BamlContext, BamlRecord>> handlerMap =
			new Dictionary<BamlRecordType, Action<BamlContext, BamlRecord>>();

		void InitRecordHandlers() {
			handlerMap[BamlRecordType.XmlnsProperty] = Thunk<XmlnsPropertyRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PresentationOptionsAttribute] = Thunk<PresentationOptionsAttributeRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PIMapping] = Thunk<PIMappingRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.AssemblyInfo] = Thunk<AssemblyInfoRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.Property] = Thunk<PropertyRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyWithConverter] = Thunk<PropertyWithConverterRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyCustom] = Thunk<PropertyCustomRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.DefAttribute] = Thunk<DefAttributeRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.DefAttributeKeyString] = Thunk<DefAttributeKeyStringRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.TypeInfo] = Thunk<TypeInfoRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.TypeSerializerInfo] = Thunk<TypeSerializerInfoRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.AttributeInfo] = Thunk<AttributeInfoRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.StringInfo] = Thunk<StringInfoRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.Text] = Thunk<TextRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.TextWithConverter] = Thunk<TextWithConverterRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.TextWithId] = Thunk<TextWithIdRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.LiteralContent] = Thunk<LiteralContentRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.RoutedEvent] = Thunk<RoutedEventRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.DocumentStart] = Thunk<DocumentStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.ElementStart] = Thunk<ElementStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.KeyElementStart] = Thunk<KeyElementStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.ConnectionId] = Thunk<ConnectionIdRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyWithExtension] = Thunk<PropertyWithExtensionRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyTypeReference] = Thunk<PropertyTypeReferenceRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyStringReference] = Thunk<PropertyStringReferenceRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyWithStaticResourceId] = Thunk<PropertyWithStaticResourceIdRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.ContentProperty] = Thunk<ContentPropertyRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.DefAttributeKeyType] = Thunk<DefAttributeKeyTypeRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyListStart] = Thunk<PropertyListStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyDictionaryStart] = Thunk<PropertyDictionaryStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyArrayStart] = Thunk<PropertyArrayStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.PropertyComplexStart] = Thunk<PropertyComplexStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.ConstructorParameterType] = Thunk<ConstructorParameterTypeRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.DeferableContentStart] = Thunk<DeferableContentStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.StaticResourceStart] = Thunk<StaticResourceStartRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.StaticResourceId] = Thunk<StaticResourceIdRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.OptimizedStaticResource] = Thunk<OptimizedStaticResourceRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.LineNumberAndPosition] = Thunk<LineNumberAndPositionRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.LinePosition] = Thunk<LinePositionRecord>(DisassembleRecord);
			handlerMap[BamlRecordType.NamedElementStart] = Thunk<NamedElementStartRecord>(DisassembleRecord);
		}

		#endregion

		ILanguage lang;
		ITextOutput output;
		CancellationToken token;

		public BamlDisassembler(ILanguage lang, ITextOutput output, CancellationToken token) {
			this.lang = lang;
			this.output = output;
			this.token = token;

			InitRecordHandlers();
		}

		void WriteText(string value) {
			output.Write(value, BoxedTextTokenKind.Text);
		}

		void WriteString(string value) {
			string str = NumberVMUtils.ToString(value, true);
			output.Write(str, BoxedTextTokenKind.String);
		}

		void WriteHexNumber(byte num) {
			output.Write("0x", BoxedTextTokenKind.Number);
			output.Write(num.ToString("x2", CultureInfo.InvariantCulture), BoxedTextTokenKind.Number);
		}

		void WriteHexNumber(ushort num) {
			output.Write("0x", BoxedTextTokenKind.Number);
			output.Write(num.ToString("x4", CultureInfo.InvariantCulture), BoxedTextTokenKind.Number);
		}

		void WriteHexNumber(uint num) {
			output.Write("0x", BoxedTextTokenKind.Number);
			output.Write(num.ToString("x8", CultureInfo.InvariantCulture), BoxedTextTokenKind.Number);
		}

		void WriteBool(bool value) {
			output.Write(value ? "true" : "false", BoxedTextTokenKind.Keyword);
		}

		void WriteVersion(BamlDocument.BamlVersion value) {
			output.Write("[", BoxedTextTokenKind.Text);
			WriteHexNumber(value.Major);
			output.Write(", ", BoxedTextTokenKind.Text);
			WriteHexNumber(value.Minor);
			output.Write("]", BoxedTextTokenKind.Text);
		}

		void WriteAssemblyId(BamlContext ctx, ushort id) {
			string reference;
			if (id == 0xffff)
				reference = ctx.KnownThings.FrameworkAssembly.FullName;
			else if (ctx.AssemblyIdMap.ContainsKey(id))
				reference = ctx.AssemblyIdMap[id].AssemblyFullName;
			else
				reference = null;
			output.WriteReference(string.Format("0x{0:x4}", id), BamlToolTipReference.Create(reference), BoxedTextTokenKind.Number, true);
		}

		void WriteTypeId(BamlContext ctx, ushort id) {
			string reference;
			if (id > 0x7fff)
				reference = ctx.KnownThings.Types((KnownTypes)(-id)).FullName;
			else if (ctx.TypeIdMap.ContainsKey(id))
				reference = ctx.TypeIdMap[id].TypeFullName;
			else
				reference = null;

			if (reference != null)
				reference = IdentifierEscaper.Escape(reference);

			output.WriteReference(string.Format("0x{0:x4}", id), BamlToolTipReference.Create(reference), BoxedTextTokenKind.Number, true);
		}

		void WriteAttributeId(BamlContext ctx, ushort id) {
			string declType;
			string name;
			if (id > 0x7fff) {
				var knownMember = ctx.KnownThings.Members((KnownMembers)(-id));
				declType = knownMember.DeclaringType.FullName;
				name = knownMember.Name;
			}
			else if (ctx.AttributeIdMap.ContainsKey(id)) {
				var attrInfo = ctx.AttributeIdMap[id];
				if (attrInfo.OwnerTypeId > 0x7fff)
					declType = ctx.KnownThings.Types((KnownTypes)(-attrInfo.OwnerTypeId)).FullName;
				else if (ctx.TypeIdMap.ContainsKey(attrInfo.OwnerTypeId))
					declType = ctx.TypeIdMap[attrInfo.OwnerTypeId].TypeFullName;
				else
					declType = string.Format("(0x{0:x4})", attrInfo.OwnerTypeId);
				name = attrInfo.Name;
			}
			else
				declType = name = null;

			string reference = null;
			if (declType != null && name != null)
				reference = string.Format("{0}::{1}", IdentifierEscaper.Escape(declType), IdentifierEscaper.Escape(name));
			output.WriteReference(string.Format("0x{0:x4}", id), BamlToolTipReference.Create(reference), BoxedTextTokenKind.Number, true);
		}

		void WriteStringId(BamlContext ctx, ushort id) {
			string str;
			if (id > 0x7fff)
				str = ctx.KnownThings.Strings((short)-id);
			else if (ctx.StringIdMap.ContainsKey(id))
				str = ctx.StringIdMap[id].Value;
			else
				str = null;
			string reference = null;
			if (str != null)
				reference = NumberVMUtils.ToString(str, true);
			output.WriteReference(string.Format("0x{0:x4}", id), BamlToolTipReference.Create(reference), BoxedTextTokenKind.Number, true);
		}

		void WriteDefinition(string value, string def = null) {
			string str = NumberVMUtils.ToString(value, true);
			output.WriteDefinition(str, BamlToolTipReference.Create(def ?? IdentifierEscaper.Escape(value)), BoxedTextTokenKind.String, true);
		}

		void WriteRecordRef(BamlRecord record) {
			output.WriteReference(record.Type.ToString(), BamlToolTipReference.Create(GetRecordReference(record)), BoxedTextTokenKind.Keyword, true);
		}

		public void Disassemble(ModuleDef module, BamlDocument document) {
			WriteText("Signature:      \t");
			WriteString(document.Signature);
			output.WriteLine();

			WriteText("Reader Version: \t");
			WriteVersion(document.ReaderVersion);
			output.WriteLine();

			WriteText("Updater Version:\t");
			WriteVersion(document.UpdaterVersion);
			output.WriteLine();

			WriteText("Writer Version: \t");
			WriteVersion(document.WriterVersion);
			output.WriteLine();

			WriteText("Record #:       \t");
			output.Write(document.Count.ToString(CultureInfo.InvariantCulture), BoxedTextTokenKind.Number);
			output.WriteLine();

			output.WriteLine();

			var ctx = BamlContext.ConstructContext(module, document, token);
			scopeStack.Clear();
			foreach (var record in document) {
				token.ThrowIfCancellationRequested();
				DisassembleRecord(ctx, record);
			}
		}

		static string GetRecordReference(BamlRecord record) {
			return string.Format("Position: 0x{0:x}", record.Position);
		}

		Stack<BamlRecord> scopeStack = new Stack<BamlRecord>();

		void DisassembleRecord(BamlContext ctx, BamlRecord record) {
			if (BamlNode.IsFooter(record)) {
				while (scopeStack.Count > 0 && !BamlNode.IsMatch(scopeStack.Peek(), record)) {
					scopeStack.Pop();
					output.Unindent();
				}
				if (scopeStack.Count > 0) {
					scopeStack.Pop();
					output.Unindent();
				}
			}

			output.WriteDefinition(record.Type.ToString(), BamlToolTipReference.Create(GetRecordReference(record)), BoxedTextTokenKind.Keyword, true);

			Action<BamlContext, BamlRecord> handler;
			if (handlerMap.TryGetValue(record.Type, out handler)) {
				output.Write(" [", BoxedTextTokenKind.Text);
				handler(ctx, record);
				output.Write("]", BoxedTextTokenKind.Text);
			}

			output.WriteLine();

			if (BamlNode.IsHeader(record)) {
				scopeStack.Push(record);
				output.Indent();
			}
		}

		#region Record handlers

		void DisassembleRecord(BamlContext ctx, XmlnsPropertyRecord record) {
			WriteText("Prefix=");
			WriteString(record.Prefix);

			WriteText(", XmlNamespace=");
			WriteString(record.XmlNamespace);

			WriteText(", AssemblyIds={");
			for (int i = 0; i < record.AssemblyIds.Length; i++) {
				if (i != 0)
					WriteText(", ");
				WriteAssemblyId(ctx, record.AssemblyIds[i]);
			}
			WriteText("}");
		}

		void DisassembleRecord(BamlContext ctx, PresentationOptionsAttributeRecord record) {
			WriteText("Value=");
			WriteString(record.Value);

			WriteText(", NameId=");
			WriteStringId(ctx, record.NameId);
		}

		void DisassembleRecord(BamlContext ctx, PIMappingRecord record) {
			WriteText("XmlNamespace=");
			WriteString(record.XmlNamespace);

			WriteText(", ClrNamespace=");
			WriteString(record.ClrNamespace);

			WriteText(", AssemblyId=");
			WriteAssemblyId(ctx, record.AssemblyId);
		}

		void DisassembleRecord(BamlContext ctx, AssemblyInfoRecord record) {
			WriteText("AssemblyId=");
			WriteHexNumber(record.AssemblyId);

			WriteText(", AssemblyFullName=");
			WriteDefinition(record.AssemblyFullName);
		}

		void DisassembleRecord(BamlContext ctx, PropertyRecord record) {
			WriteText("AttributeId=");
			WriteAttributeId(ctx, record.AttributeId);

			WriteText(", Value=");
			WriteString(record.Value);
		}

		void DisassembleRecord(BamlContext ctx, PropertyWithConverterRecord record) {
			DisassembleRecord(ctx, (PropertyRecord)record);

			WriteText(", ConverterTypeId=");
			WriteTypeId(ctx, record.ConverterTypeId);
		}

		void DisassembleRecord(BamlContext ctx, PropertyCustomRecord record) {
			WriteText("AttributeId=");
			WriteAttributeId(ctx, record.AttributeId);

			WriteText(", SerializerTypeId=");
			WriteTypeId(ctx, record.SerializerTypeId);

			WriteText(", Data=");
			for (int i = 0; i < record.Data.Length; i++)
				output.Write(record.Data[i].ToString("x2"), BoxedTextTokenKind.String);
		}

		void DisassembleRecord(BamlContext ctx, DefAttributeRecord record) {
			WriteText("Value=");
			WriteString(record.Value);

			WriteText(", NameId=");
			WriteStringId(ctx, record.NameId);
		}

		void DisassembleRecord(BamlContext ctx, DefAttributeKeyStringRecord record) {
			WriteText("ValueId=");
			WriteStringId(ctx, record.ValueId);

			WriteText(", Shared=");
			WriteBool(record.Shared);

			WriteText(", SharedSet=");
			WriteBool(record.SharedSet);

			WriteText(", Record=");
			WriteRecordRef(record.Record);
		}

		void DisassembleRecord(BamlContext ctx, TypeInfoRecord record) {
			WriteText("TypeId=");
			WriteHexNumber(record.TypeId);

			WriteText(", AssemblyId=");
			WriteAssemblyId(ctx, record.AssemblyId);

			WriteText(", TypeFullName=");
			WriteDefinition(record.TypeFullName);
		}

		void DisassembleRecord(BamlContext ctx, TypeSerializerInfoRecord record) {
			DisassembleRecord(ctx, (TypeInfoRecord)record);

			WriteText(", SerializerTypeId=");
			WriteTypeId(ctx, record.SerializerTypeId);
		}

		void DisassembleRecord(BamlContext ctx, AttributeInfoRecord record) {
			WriteText("AttributeId=");
			WriteHexNumber(record.AttributeId);

			WriteText(", OwnerTypeId=");
			WriteTypeId(ctx, record.OwnerTypeId);

			WriteText(", AttributeUsage=");
			WriteHexNumber(record.AttributeUsage);

			string declType;
			if (record.OwnerTypeId > 0x7fff)
				declType = ctx.KnownThings.Types((KnownTypes)(-record.OwnerTypeId)).FullName;
			else if (ctx.TypeIdMap.ContainsKey(record.OwnerTypeId))
				declType = ctx.TypeIdMap[record.OwnerTypeId].TypeFullName;
			else
				declType = string.Format("(0x{0:x4})", record.OwnerTypeId);
			var def = string.Format("{0}::{1}", IdentifierEscaper.Escape(declType), IdentifierEscaper.Escape(record.Name));

			WriteText(", Name=");
			WriteDefinition(record.Name, def);
		}

		void DisassembleRecord(BamlContext ctx, StringInfoRecord record) {
			WriteText("StringId=");
			WriteHexNumber(record.StringId);

			WriteText(", Value=");
			WriteString(record.Value);
		}

		void DisassembleRecord(BamlContext ctx, TextRecord record) {
			WriteText("Value=");
			WriteString(record.Value);
		}

		void DisassembleRecord(BamlContext ctx, TextWithConverterRecord record) {
			DisassembleRecord(ctx, (TextRecord)record);

			WriteText(", ConverterTypeId=");
			WriteTypeId(ctx, record.ConverterTypeId);
		}

		void DisassembleRecord(BamlContext ctx, TextWithIdRecord record) {
			WriteText("ValueId=");
			WriteStringId(ctx, record.ValueId);
		}

		void DisassembleRecord(BamlContext ctx, LiteralContentRecord record) {
			WriteText("Value=");
			WriteString(record.Value);

			WriteText(", Reserved0=");
			WriteHexNumber(record.Reserved0);

			WriteText(", Reserved1=");
			WriteHexNumber(record.Reserved1);
		}

		void DisassembleRecord(BamlContext ctx, RoutedEventRecord record) {
			WriteText("Value=");
			WriteString(record.Value);

			WriteText(", AttributeId=");
			WriteAttributeId(ctx, record.AttributeId);

			WriteText(", Reserved1=");
			WriteHexNumber(record.Reserved1);
		}

		void DisassembleRecord(BamlContext ctx, DocumentStartRecord record) {
			WriteText("LoadAsync=");
			WriteBool(record.LoadAsync);

			WriteText(", MaxAsyncRecords=");
			WriteHexNumber(record.MaxAsyncRecords);

			WriteText(", DebugBaml=");
			WriteBool(record.DebugBaml);
		}

		void DisassembleRecord(BamlContext ctx, ElementStartRecord record) {
			WriteText("TypeId=");
			WriteTypeId(ctx, record.TypeId);

			WriteText(", Flags=");
			WriteHexNumber(record.Flags);
		}

		void DisassembleRecord(BamlContext ctx, ConnectionIdRecord record) {
			WriteText("ConnectionId=");
			WriteHexNumber(record.ConnectionId);
		}

		void DisassembleRecord(BamlContext ctx, PropertyWithExtensionRecord record) {
			WriteText("AttributeId=");
			WriteAttributeId(ctx, record.AttributeId);

			WriteText(", Flags=");
			WriteHexNumber(record.Flags);

			WriteText(", ValueId=");
			WriteHexNumber(record.ValueId);
		}

		void DisassembleRecord(BamlContext ctx, PropertyTypeReferenceRecord record) {
			DisassembleRecord(ctx, (PropertyComplexStartRecord)record);

			WriteText(", TypeId=");
			WriteTypeId(ctx, record.TypeId);
		}

		void DisassembleRecord(BamlContext ctx, PropertyStringReferenceRecord record) {
			DisassembleRecord(ctx, (PropertyComplexStartRecord)record);

			WriteText(", StringId=");
			WriteStringId(ctx, record.StringId);
		}

		void DisassembleRecord(BamlContext ctx, PropertyWithStaticResourceIdRecord record) {
			WriteText("AttributeId=");
			WriteAttributeId(ctx, record.AttributeId);
			WriteText(", ");

			DisassembleRecord(ctx, (StaticResourceIdRecord)record);
		}

		void DisassembleRecord(BamlContext ctx, ContentPropertyRecord record) {
			WriteText("AttributeId=");
			WriteAttributeId(ctx, record.AttributeId);
		}

		void DisassembleRecord(BamlContext ctx, DefAttributeKeyTypeRecord record) {
			DisassembleRecord(ctx, (ElementStartRecord)record);

			WriteText(", Shared=");
			WriteBool(record.Shared);

			WriteText(", SharedSet=");
			WriteBool(record.SharedSet);

			WriteText(", Record=");
			WriteRecordRef(record.Record);
		}

		void DisassembleRecord(BamlContext ctx, PropertyComplexStartRecord record) {
			WriteText("AttributeId=");
			WriteAttributeId(ctx, record.AttributeId);
		}

		void DisassembleRecord(BamlContext ctx, ConstructorParameterTypeRecord record) {
			WriteText("TypeId=");
			WriteTypeId(ctx, record.TypeId);
		}

		void DisassembleRecord(BamlContext ctx, DeferableContentStartRecord record) {
			WriteText("Record=");
			WriteRecordRef(record.Record);
		}

		void DisassembleRecord(BamlContext ctx, StaticResourceIdRecord record) {
			WriteText("StaticResourceId=");
			WriteHexNumber(record.StaticResourceId);
		}

		void DisassembleRecord(BamlContext ctx, OptimizedStaticResourceRecord record) {
			WriteText("Flags=");
			WriteHexNumber(record.Flags);

			WriteText(", ValueId=");
			if (record.IsType)
				WriteTypeId(ctx, record.ValueId);
			else if (record.IsStatic)
				WriteAttributeId(ctx, record.ValueId);
			else
				WriteStringId(ctx, record.ValueId);
		}

		void DisassembleRecord(BamlContext ctx, LineNumberAndPositionRecord record) {
			WriteText("LineNumber=");
			WriteHexNumber(record.LineNumber);

			WriteText(", LinePosition=");
			WriteHexNumber(record.LinePosition);
		}

		void DisassembleRecord(BamlContext ctx, LinePositionRecord record) {
			WriteText("LinePosition=");
			WriteHexNumber(record.LinePosition);
		}

		void DisassembleRecord(BamlContext ctx, NamedElementStartRecord record) {
			WriteText("TypeId=");
			WriteTypeId(ctx, record.TypeId);

			WriteText(", RuntimeName=");
			WriteString(record.RuntimeName);
		}

		#endregion
	}
}