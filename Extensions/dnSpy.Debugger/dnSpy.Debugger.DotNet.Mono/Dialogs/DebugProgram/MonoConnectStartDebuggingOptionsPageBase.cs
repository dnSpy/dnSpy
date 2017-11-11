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
using System.ComponentModel;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Contracts.Debugger.StartDebugging.Dialog;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.DotNet.Mono.Dialogs.DebugProgram {
	abstract class MonoConnectStartDebuggingOptionsPageBase : StartDebuggingOptionsPage, IDataErrorInfo {
		public sealed override object UIObject => this;

		public string Address {
			get => address;
			set {
				if (address != value) {
					address = value;
					OnPropertyChanged(nameof(Address));
					UpdateIsValid();
				}
			}
		}
		string address;

		public UInt16VM ConnectionPort { get; }

		public EnumListVM BreakProcessKindVM => breakProcessKindVM;
		readonly EnumListVM breakProcessKindVM = new EnumListVM(BreakProcessKindsUtils.BreakProcessKindList);

		public string BreakKind {
			get => (string)BreakProcessKindVM.SelectedItem;
			set => BreakProcessKindVM.SelectedItem = value;
		}

		public override bool IsValid => isValid;
		bool isValid;

		void UpdateIsValid() {
			var newIsValid = CalculateIsValid();
			if (newIsValid == isValid)
				return;
			isValid = newIsValid;
			OnPropertyChanged(nameof(IsValid));
		}

		protected MonoConnectStartDebuggingOptionsPageBase() {
			ConnectionPort = new UInt16VM(a => UpdateIsValid(), useDecimal: true) { Min = 1 };
		}

		static string FilterBreakKind(string breakKind) {
			foreach (var info in BreakProcessKindsUtils.BreakProcessKindList) {
				if (StringComparer.Ordinal.Equals(breakKind, (string)info.Value))
					return breakKind;
			}
			return PredefinedBreakKinds.DontBreak;
		}

		void Initialize(MonoConnectStartDebuggingOptions options) {
			Address = options.Address;
			ConnectionPort.Value = options.Port;
			BreakKind = FilterBreakKind(options.BreakKind);
		}

		MonoConnectStartDebuggingOptions InitializeDefault(MonoConnectStartDebuggingOptions options, string breakKind) {
			options.BreakKind = FilterBreakKind(breakKind);
			return options;
		}

		MonoConnectStartDebuggingOptions GetOptions(MonoConnectStartDebuggingOptions options) {
			options.Address = Address;
			options.Port = ConnectionPort.Value;
			options.BreakKind = FilterBreakKind(BreakKind);
			return options;
		}

		string IDataErrorInfo.Error => throw new NotImplementedException();
		string IDataErrorInfo.this[string columnName] => Verify(columnName);

		public override void InitializePreviousOptions(StartDebuggingOptions options) {
			var dncOptions = options as MonoConnectStartDebuggingOptions;
			if (dncOptions == null)
				return;
			Initialize(dncOptions);
		}

		public override void InitializeDefaultOptions(string filename, string breakKind, StartDebuggingOptions options) =>
			Initialize(GetDefaultOptions(filename, breakKind, options));

		MonoConnectStartDebuggingOptions GetDefaultOptions(string filename, string breakKind, StartDebuggingOptions options) {
			if (options is MonoConnectStartDebuggingOptions connectOptions)
				return connectOptions;
			return CreateOptions(breakKind);
		}

		MonoConnectStartDebuggingOptions CreateOptions(string breakKind) =>
			InitializeDefault(new MonoConnectStartDebuggingOptions(), breakKind);

		public override StartDebuggingOptionsInfo GetOptions() {
			var options = GetOptions(new MonoConnectStartDebuggingOptions());
			return new StartDebuggingOptionsInfo(options);
		}

		public override bool SupportsDebugEngine(Guid engineGuid, out double order) {
			order = 0;
			return false;
		}

		bool CalculateIsValid() =>
			!ConnectionPort.HasError &&
			string.IsNullOrEmpty(Verify(nameof(Address)));

		string Verify(string columnName) {
			return string.Empty;
		}
	}
}
