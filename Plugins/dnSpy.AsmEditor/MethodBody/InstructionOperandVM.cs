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
using System.Diagnostics;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.AsmEditor.Properties;
using dnSpy.Shared.MVVM;

namespace dnSpy.AsmEditor.MethodBody {
	enum InstructionOperandType {
		None,
		SByte,
		Byte,
		Int32,
		Int64,
		Single,
		Double,
		String,
		Field,
		Method,
		Token,
		Type,
		MethodSig,
		BranchTarget,
		SwitchTargets,
		Local,
		Parameter,
	}

	sealed class InstructionOperandVM : ViewModelBase {
		public IEditOperand EditOperand {
			set { editOperand = value; }
		}
		IEditOperand editOperand;

		public ICommand EditOtherCommand {
			get { return new RelayCommand(a => EditOther(a), a => EditOtherCanExecute(a)); }
		}

		public InstructionOperandType InstructionOperandType {
			get { return operandType; }
			set {
				if (operandType != value) {
					operandType = value;
					OnPropertyChanged("InstructionOperandType");
					OnPropertyChanged("Text");
					OnPropertyChanged("Other");
					OnPropertyChanged("StringIsSelected");
					FieldUpdated();
				}
			}
		}
		InstructionOperandType operandType;

		public object Value {
			get {
				switch (InstructionOperandType) {
				case MethodBody.InstructionOperandType.None:	return null;
				case MethodBody.InstructionOperandType.SByte:	return SByte.Value;
				case MethodBody.InstructionOperandType.Byte:	return Byte.Value;
				case MethodBody.InstructionOperandType.Int32:	return Int32.Value;
				case MethodBody.InstructionOperandType.Int64:	return Int64.Value;
				case MethodBody.InstructionOperandType.Single:	return Single.Value;
				case MethodBody.InstructionOperandType.Double:	return Double.Value;
				case MethodBody.InstructionOperandType.String:	return String.Value;

				case MethodBody.InstructionOperandType.BranchTarget:
				case MethodBody.InstructionOperandType.Local:
				case MethodBody.InstructionOperandType.Parameter:
					if (BodyUtils.IsNull(OperandListItem))
						return null;
					return OperandListItem;

				case MethodBody.InstructionOperandType.Field:
				case MethodBody.InstructionOperandType.Method:
				case MethodBody.InstructionOperandType.Token:
				case MethodBody.InstructionOperandType.Type:
				case MethodBody.InstructionOperandType.MethodSig:
				case MethodBody.InstructionOperandType.SwitchTargets:
					return Other;

				default: throw new InvalidOperationException();
				}
			}
		}

		public void SwitchOperandChanged() {
			OnPropertyChanged("Other");
		}

		public void BranchOperandChanged(IEnumerable<InstructionVM> instrs) {
			if (OperandListVM.Items.Count > 0 && !(OperandListVM.SelectedItem is InstructionVM))
				OperandListVM.SelectedItem = InstructionVM.Null;
			OperandListVM.InvalidateSelected(instrs, false, InstructionVM.Null);
			if (OperandListVM.Items.Count > 0 && OperandListVM.SelectedItem == InstructionVM.Null)
				OperandListVM.SelectedIndex = 0;
		}

		public void LocalOperandChanged(IEnumerable<LocalVM> locals) {
			if (OperandListVM.Items.Count > 0 && !(OperandListVM.SelectedItem is LocalVM))
				OperandListVM.SelectedItem = LocalVM.Null;
			OperandListVM.InvalidateSelected(locals, false, LocalVM.Null);
			if (OperandListVM.Items.Count > 0 && OperandListVM.SelectedItem == LocalVM.Null)
				OperandListVM.SelectedIndex = 0;
		}

		public void ParameterOperandChanged(IEnumerable<Parameter> parameters) {
			if (OperandListVM.Items.Count > 0 && !(OperandListVM.SelectedItem is Parameter))
				OperandListVM.SelectedItem = BodyUtils.NullParameter;
			OperandListVM.InvalidateSelected(parameters, false, BodyUtils.NullParameter);
			if (OperandListVM.Items.Count > 0 && OperandListVM.SelectedItem == BodyUtils.NullParameter)
				OperandListVM.SelectedIndex = 0;
		}

		public void UpdateOperandType(Code code) {
			InstructionOperandType = GetOperandType(code);
		}

		public void WriteValue(Code code, object value) {
			UpdateOperandType(code);
			switch (InstructionOperandType) {
			case MethodBody.InstructionOperandType.None:	break;
			case MethodBody.InstructionOperandType.SByte:	SByte.Value = (sbyte)value; break;
			case MethodBody.InstructionOperandType.Byte:	Byte.Value = (byte)value; break;
			case MethodBody.InstructionOperandType.Int32:	Int32.Value = (int)value; break;
			case MethodBody.InstructionOperandType.Int64:	Int64.Value = (long)value; break;
			case MethodBody.InstructionOperandType.Single:	Single.Value = (float)value; break;
			case MethodBody.InstructionOperandType.Double:	Double.Value = (double)value; break;
			case MethodBody.InstructionOperandType.String:	String.Value = (string)value; break;
			case MethodBody.InstructionOperandType.Field:	Other = (IField)value; break;
			case MethodBody.InstructionOperandType.Method:	Other = (IMethod)value; break;
			case MethodBody.InstructionOperandType.Token:	Other = (ITokenOperand)value; break;
			case MethodBody.InstructionOperandType.Type:	Other = (ITypeDefOrRef)value; break;
			case MethodBody.InstructionOperandType.MethodSig: Other = (MethodSig)value; break;
			case MethodBody.InstructionOperandType.BranchTarget: OperandListItem = (InstructionVM)value ?? InstructionVM.Null; break;
			case MethodBody.InstructionOperandType.SwitchTargets: Other = (InstructionVM[])value; break;
			case MethodBody.InstructionOperandType.Local:	OperandListItem = (LocalVM)value ?? LocalVM.Null; break;
			case MethodBody.InstructionOperandType.Parameter: OperandListItem = (Parameter)value ?? BodyUtils.NullParameter; break;
			default: throw new InvalidOperationException();
			}
		}

		static MethodBody.InstructionOperandType GetOperandType(Code code) {
			switch (code.ToOpCode().OperandType) {
			case OperandType.InlineBrTarget:		return MethodBody.InstructionOperandType.BranchTarget;
			case OperandType.InlineField:			return MethodBody.InstructionOperandType.Field;
			case OperandType.InlineI:				return MethodBody.InstructionOperandType.Int32;
			case OperandType.InlineI8:				return MethodBody.InstructionOperandType.Int64;
			case OperandType.InlineMethod:			return MethodBody.InstructionOperandType.Method;
			case OperandType.InlineNone:			return MethodBody.InstructionOperandType.None;
			case OperandType.InlinePhi:				return MethodBody.InstructionOperandType.None;
			case OperandType.InlineR:				return MethodBody.InstructionOperandType.Double;
			case OperandType.InlineSig:				return MethodBody.InstructionOperandType.MethodSig;
			case OperandType.InlineString:			return MethodBody.InstructionOperandType.String;
			case OperandType.InlineSwitch:			return MethodBody.InstructionOperandType.SwitchTargets;
			case OperandType.InlineTok:				return MethodBody.InstructionOperandType.Token;
			case OperandType.InlineType:			return MethodBody.InstructionOperandType.Type;
			case OperandType.ShortInlineBrTarget:	return MethodBody.InstructionOperandType.BranchTarget;
			case OperandType.ShortInlineR:			return MethodBody.InstructionOperandType.Single;
			default:								return MethodBody.InstructionOperandType.None;

			case OperandType.ShortInlineI:
				if (code == Code.Ldc_I4_S)
					return MethodBody.InstructionOperandType.SByte;
				return MethodBody.InstructionOperandType.Byte;

			case OperandType.InlineVar:
			case OperandType.ShortInlineVar:
				switch (code) {
				case Code.Ldloc:
				case Code.Ldloc_S:
				case Code.Ldloca:
				case Code.Ldloca_S:
				case Code.Stloc:
				case Code.Stloc_S:
					return MethodBody.InstructionOperandType.Local;

				case Code.Ldarg:
				case Code.Ldarg_S:
				case Code.Ldarga:
				case Code.Ldarga_S:
				case Code.Starg:
				case Code.Starg_S:
					return MethodBody.InstructionOperandType.Parameter;

				default:
					throw new InvalidOperationException();
				}
			}
		}

		public DataFieldVM Text {
			get {
				switch (InstructionOperandType) {
				case MethodBody.InstructionOperandType.SByte:	return SByte;
				case MethodBody.InstructionOperandType.Byte:	return Byte;
				case MethodBody.InstructionOperandType.Int32:	return Int32;
				case MethodBody.InstructionOperandType.Int64:	return Int64;
				case MethodBody.InstructionOperandType.Single:	return Single;
				case MethodBody.InstructionOperandType.Double:	return Double;
				case MethodBody.InstructionOperandType.String:	return String;

				case MethodBody.InstructionOperandType.None:
				case MethodBody.InstructionOperandType.Field:
				case MethodBody.InstructionOperandType.Method:
				case MethodBody.InstructionOperandType.Token:
				case MethodBody.InstructionOperandType.Type:
				case MethodBody.InstructionOperandType.MethodSig:
				case MethodBody.InstructionOperandType.BranchTarget:
				case MethodBody.InstructionOperandType.SwitchTargets:
				case MethodBody.InstructionOperandType.Local:
				case MethodBody.InstructionOperandType.Parameter:
					return null;

				default: throw new InvalidOperationException();
				}
			}
		}

		public bool StringIsSelected {
			get { return InstructionOperandType == MethodBody.InstructionOperandType.String; }
		}

		public SByteVM SByte {
			get { return @sbyte; }
		}
		SByteVM @sbyte;

		public ByteVM Byte {
			get { return @byte; }
		}
		ByteVM @byte;

		public Int32VM Int32 {
			get { return int32; }
		}
		Int32VM int32;

		public Int64VM Int64 {
			get { return int64; }
		}
		Int64VM int64;

		public SingleVM Single {
			get { return single; }
		}
		SingleVM single;

		public DoubleVM Double {
			get { return @double; }
		}
		DoubleVM @double;

		public StringVM String {
			get { return @string; }
		}
		StringVM @string;

		public object Other {
			get { return other; }
			set {
				if (other != value) {
					other = value;
					OnPropertyChanged("Other");
					FieldUpdated();
				}
			}
		}
		object other;

		public object OperandListItem {
			get { return operandListVM.SelectedItem; }
			set { operandListVM.SelectedItem = value; }
		}

		public ListVM<object> OperandListVM {
			get { return operandListVM; }
		}
		readonly ListVM<object> operandListVM;

		public InstructionOperandVM() {
			this.@sbyte = new SByteVM(a => FieldUpdated());
			this.@byte = new ByteVM(a => FieldUpdated());
			this.int32 = new Int32VM(a => FieldUpdated());
			this.int64 = new Int64VM(a => FieldUpdated());
			this.single = new SingleVM(a => FieldUpdated());
			this.@double = new DoubleVM(a => FieldUpdated());
			this.@string = new StringVM(a => FieldUpdated());
			this.operandListVM = new ListVM<object>((a, b) => FieldUpdated());
			this.OperandListVM.DataErrorInfoDelegate = VerifyOperand;
		}

		void EditOther(object parameter) {
			switch (InstructionOperandType) {
			case InstructionOperandType.Field:
			case InstructionOperandType.Method:
			case InstructionOperandType.Token:
			case InstructionOperandType.Type:
			case InstructionOperandType.MethodSig:
			case InstructionOperandType.SwitchTargets:
				if (editOperand == null)
					throw new InvalidOperationException();
				editOperand.Edit(parameter, this);
				break;

			default:
				break;
			}
		}

		bool EditOtherCanExecute(object parameter) {
			switch (InstructionOperandType) {
			case InstructionOperandType.Field:
			case InstructionOperandType.Method:
			case InstructionOperandType.Token:
			case InstructionOperandType.Type:
			case InstructionOperandType.MethodSig:
			case InstructionOperandType.SwitchTargets:
				return true;

			default:
				return false;
			}
		}

		string VerifyOperand(ListVM<object> list) {
			var item = list.SelectedItem;

			switch (InstructionOperandType) {
			case MethodBody.InstructionOperandType.BranchTarget:
				var instr = item as InstructionVM;
				if (instr == null || instr == InstructionVM.Null)
					return dnSpy_AsmEditor_Resources.Error_OpMustBeInstr;
				if (instr.Index == -1)
					return dnSpy_AsmEditor_Resources.Error_InstrTargetHasBeenRemoved;
				break;

			case MethodBody.InstructionOperandType.Local:
				var local = item as LocalVM;
				if (local == null || local == LocalVM.Null)
					return dnSpy_AsmEditor_Resources.Error_OpMustBeLocal;
				if (local.Index == -1)
					return dnSpy_AsmEditor_Resources.Error_LocalHasBeenRemoved;
				break;

			case MethodBody.InstructionOperandType.Parameter:
				var p = item as Parameter;
				if (p == null || p == BodyUtils.NullParameter)
					return dnSpy_AsmEditor_Resources.Error_OpMustBeParam;
				break;

			default:
				break;
			}

			return string.Empty;
		}

		bool HasListError(ListVM<object> list) {
			return !string.IsNullOrEmpty(VerifyOperand(list));
		}

		void FieldUpdated() {
			OnPropertyChanged("Modified");
			HasErrorUpdated();
		}

		public void InitializeFrom(InstructionOperandVM other) {
			this.InstructionOperandType = other.InstructionOperandType;
			this.SByte.StringValue = other.SByte.StringValue;
			this.Byte.StringValue = other.Byte.StringValue;
			this.Int32.StringValue = other.Int32.StringValue;
			this.Int64.StringValue = other.Int64.StringValue;
			this.Single.StringValue = other.Single.StringValue;
			this.Double.StringValue = other.Double.StringValue;
			this.String.StringValue = other.String.StringValue;
			this.Other = other.Other;
			this.OperandListItem = other.OperandListItem;
		}

		public void ImportFrom(ModuleDef ownerModule, InstructionOperandVM other) {
			this.InstructionOperandType = other.InstructionOperandType;

			switch (other.InstructionOperandType) {
			case MethodBody.InstructionOperandType.None:	break;
			case MethodBody.InstructionOperandType.SByte:	SByte.StringValue = other.SByte.StringValue; break;
			case MethodBody.InstructionOperandType.Byte:	Byte.StringValue = other.Byte.StringValue; break;
			case MethodBody.InstructionOperandType.Int32:	Int32.StringValue = other.Int32.StringValue; break;
			case MethodBody.InstructionOperandType.Int64:	Int64.StringValue = other.Int64.StringValue; break;
			case MethodBody.InstructionOperandType.Single:	Single.StringValue = other.Single.StringValue; break;
			case MethodBody.InstructionOperandType.Double:	Double.StringValue = other.Double.StringValue; break;
			case MethodBody.InstructionOperandType.String:	String.StringValue = other.String.StringValue; break;
			case MethodBody.InstructionOperandType.Field:	Other = Import(ownerModule, other.Other); break;
			case MethodBody.InstructionOperandType.Method:	Other = Import(ownerModule, other.Other); break;
			case MethodBody.InstructionOperandType.Token:	Other = Import(ownerModule, other.Other); break;
			case MethodBody.InstructionOperandType.Type:	Other = Import(ownerModule, other.Other); break;
			case MethodBody.InstructionOperandType.MethodSig: Other = Import(ownerModule, other.Other); break;
			case MethodBody.InstructionOperandType.BranchTarget: OperandListItem = InstructionVM.Null; break;
			case MethodBody.InstructionOperandType.SwitchTargets: Other = new InstructionVM[0]; break;
			case MethodBody.InstructionOperandType.Local:	OperandListItem = LocalVM.Null; break;
			case MethodBody.InstructionOperandType.Parameter: OperandListItem = BodyUtils.NullParameter; break;
			default: throw new InvalidOperationException();
			}
		}

		object Import(ModuleDef ownerModule, object o) {
			var importer = new Importer(ownerModule, ImporterOptions.TryToUseDefs);

			var tdr = o as ITypeDefOrRef;
			if (tdr != null)
				return importer.Import(tdr);

			var method = o as IMethod;
			if (method != null && method.IsMethod)
				return importer.Import(method);

			var field = o as IField;
			if (field != null && field.IsField)
				return importer.Import(field);

			var msig = o as MethodSig;
			if (msig != null)
				return importer.Import(msig);

			Debug.Assert(o == null);
			return null;
		}

		protected override string Verify(string columnName) {
			if (columnName == "Other") {
				switch (InstructionOperandType) {
				case MethodBody.InstructionOperandType.None:
				case MethodBody.InstructionOperandType.SByte:
				case MethodBody.InstructionOperandType.Byte:
				case MethodBody.InstructionOperandType.Int32:
				case MethodBody.InstructionOperandType.Int64:
				case MethodBody.InstructionOperandType.Single:
				case MethodBody.InstructionOperandType.Double:
				case MethodBody.InstructionOperandType.String:
				case MethodBody.InstructionOperandType.BranchTarget:
				case MethodBody.InstructionOperandType.Local:
				case MethodBody.InstructionOperandType.Parameter:
					break;

				case MethodBody.InstructionOperandType.Field:
					if (Other != null) {
						if (!(Other is IField))
							return dnSpy_AsmEditor_Resources.Error_OpMustBeField;
						var method = Other as IMethod;
						if (method != null && method.MethodSig != null)
							return dnSpy_AsmEditor_Resources.Error_OpMustBeField;
					}
					break;

				case MethodBody.InstructionOperandType.Method:
					if (Other != null) {
						if (!(Other is IMethod))
							return dnSpy_AsmEditor_Resources.Error_OpMustBeMethod;
						var field = Other as IField;
						if (field != null && field.FieldSig != null)
							return dnSpy_AsmEditor_Resources.Error_OpMustBeMethod;
					}
					break;

				case MethodBody.InstructionOperandType.Token:
					if (Other != null && !(Other is ITokenOperand)) return dnSpy_AsmEditor_Resources.Error_OpMustBeTypeMethodField;
					break;

				case MethodBody.InstructionOperandType.Type:
					if (Other != null && !(Other is ITypeDefOrRef)) return dnSpy_AsmEditor_Resources.Error_OpMustBeType;
					break;

				case MethodBody.InstructionOperandType.MethodSig:
					if (Other != null && !(Other is MethodSig)) return dnSpy_AsmEditor_Resources.Error_OpMustBeMethodSig;
					break;

				case MethodBody.InstructionOperandType.SwitchTargets:
					var list = Other as IList<InstructionVM>;
					if (list == null)
						return dnSpy_AsmEditor_Resources.Error_OpMustBeListInstrs;
					foreach (var i in list) {
						if (i != null && i.Index == -1)
							return dnSpy_AsmEditor_Resources.Error_SwitchInstrTargetHasBeenRemoved;
					}
					break;

				default:
					throw new InvalidOperationException();
				}
			}
			return string.Empty;
		}

		public override bool HasError {
			get {
				switch (InstructionOperandType) {
				case MethodBody.InstructionOperandType.None:
					break;

				case MethodBody.InstructionOperandType.SByte:
					if (SByte.HasError) return true;
					break;

				case MethodBody.InstructionOperandType.Byte:
					if (Byte.HasError) return true;
					break;

				case MethodBody.InstructionOperandType.Int32:
					if (Int32.HasError) return true;
					break;

				case MethodBody.InstructionOperandType.Int64:
					if (Int64.HasError) return true;
					break;

				case MethodBody.InstructionOperandType.Single:
					if (Single.HasError) return true;
					break;

				case MethodBody.InstructionOperandType.Double:
					if (Double.HasError) return true;
					break;

				case MethodBody.InstructionOperandType.String:
					if (String.HasError) return true;
					break;

				case MethodBody.InstructionOperandType.Field:
				case MethodBody.InstructionOperandType.Method:
				case MethodBody.InstructionOperandType.Token:
				case MethodBody.InstructionOperandType.Type:
				case MethodBody.InstructionOperandType.MethodSig:
				case MethodBody.InstructionOperandType.SwitchTargets:
					if (!string.IsNullOrEmpty(Verify("Other"))) return true;
					break;

				case MethodBody.InstructionOperandType.BranchTarget:
				case MethodBody.InstructionOperandType.Local:
				case MethodBody.InstructionOperandType.Parameter:
					if (HasListError(OperandListVM)) return true;
					break;

				default:
					throw new InvalidOperationException();
				}

				return false;
			}
		}
	}
}
