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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	sealed class ExceptionConditionVM : ViewModelBase {
		public bool IsNotFirst {
			get => isNotFirst;
			set {
				if (isNotFirst == value)
					return;
				isNotFirst = value;
				OnPropertyChanged(nameof(IsNotFirst));
			}
		}
		bool isNotFirst;

		public string ConditionText {
			get => conditionText;
			set {
				if (conditionText == value)
					return;
				conditionText = value;
				OnPropertyChanged(nameof(ConditionText));
			}
		}
		string conditionText;

		public bool IsEmpty => string.IsNullOrWhiteSpace(ConditionText);

		public ICommand RemoveCommand => new RelayCommand(a => owner.RemoveCondition(this));

		sealed class ConditionTypeVM : ViewModelBase {
			public DbgExceptionConditionType Type { get; }
			public string DisplayName { get; }

			public ConditionTypeVM(DbgExceptionConditionType type, string displayName) {
				Type = type;
				DisplayName = displayName;
			}
		}
		static readonly ConditionTypeVM[] conditionTypes = new ConditionTypeVM[] {
			new ConditionTypeVM(DbgExceptionConditionType.ModuleNameEquals, dnSpy_Debugger_Resources.Exception_Conditions_ModuleNameEquals),
			new ConditionTypeVM(DbgExceptionConditionType.ModuleNameNotEquals, dnSpy_Debugger_Resources.Exception_Conditions_ModuleNameNotEquals),
		};

		public object ConditionTypes => conditionTypes;
		public object SelectedConditionType {
			get => selectedConditionType;
			set {
				if (selectedConditionType == value)
					return;
				selectedConditionType = (ConditionTypeVM)value;
				OnPropertyChanged(nameof(SelectedConditionType));
			}
		}
		ConditionTypeVM selectedConditionType;

		readonly EditExceptionConditionsVM owner;

		public ExceptionConditionVM(EditExceptionConditionsVM owner, DbgExceptionConditionSettings settings) {
			this.owner = owner;
			conditionText = settings.Condition ?? string.Empty;
			selectedConditionType = conditionTypes.FirstOrDefault(a => a.Type == settings.ConditionType) ?? conditionTypes[0];
		}

		public DbgExceptionConditionSettings ToSettings() =>
			new DbgExceptionConditionSettings(selectedConditionType.Type, conditionText);
	}

	sealed class EditExceptionConditionsVM : ViewModelBase {
		public object Conditions => conditionsList;
		readonly ObservableCollection<ExceptionConditionVM> conditionsList;

		public ICommand AddConditionCommand => new RelayCommand(a => AddCondition());

		public EditExceptionConditionsVM(IEnumerable<DbgExceptionConditionSettings> condSettings) {
			if (condSettings is null)
				throw new ArgumentNullException(nameof(condSettings));
			conditionsList = new ObservableCollection<ExceptionConditionVM>(condSettings.Select(a => new ExceptionConditionVM(this, a)));
			for (int i = 0; i < conditionsList.Count; i++)
				conditionsList[i].IsNotFirst = i != 0;
			if (conditionsList.Count == 0)
				AddCondition();
		}

		public ReadOnlyCollection<DbgExceptionConditionSettings> GetConditions() =>
			new ReadOnlyCollection<DbgExceptionConditionSettings>(conditionsList.Where(a => !a.IsEmpty).Select(a => a.ToSettings()).ToArray());

		internal void RemoveCondition(ExceptionConditionVM vm) {
			bool b = conditionsList.Remove(vm);
			Debug.Assert(b);
			if (conditionsList.Count != 0)
				conditionsList[0].IsNotFirst = false;
		}

		void AddCondition() {
			var vm = new ExceptionConditionVM(this, new DbgExceptionConditionSettings(DbgExceptionConditionType.ModuleNameNotEquals, string.Empty));
			conditionsList.Add(vm);
			vm.IsNotFirst = conditionsList.Count != 1;
		}
	}
}
