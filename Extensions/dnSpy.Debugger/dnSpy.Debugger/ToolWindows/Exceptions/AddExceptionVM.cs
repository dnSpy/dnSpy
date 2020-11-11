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
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Input;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	sealed class AddExceptionVM : ViewModelBase {
		readonly Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService;

		sealed class NameOrCodeVM : DataFieldVM<string> {
			public bool IsCode {
				set {
					if (isCode == value)
						return;
					isCode = value;
					ForceWriteStringValue(StringValue);
				}
			}
			bool isCode;

			public NameOrCodeVM(Action<DataFieldVM> onUpdated)
				: base(onUpdated) {
			}

			protected override string OnNewValue(string value) => value;

			protected override string? ConvertToValue(out string value) {
				value = StringValue;

				if (isCode) {
					TryGetCode(out var code, out var error);
					return error;
				}
				else {
					TryGetName(out var name, out var error);
					return error;
				}
			}

			public bool TryGetCode(out int code, [NotNullWhen(false)] out string? error) {
				code = SimpleTypeConverter.ParseInt32(StringValue, int.MinValue, int.MaxValue, out error);
				if (error is not null)
					code = (int)SimpleTypeConverter.ParseUInt32(StringValue, uint.MinValue, uint.MaxValue, out error);
				return error is null;
			}

			public bool TryGetName([NotNullWhen(true)] out string? name, [NotNullWhen(false)] out string? error) {
				name = StringValue;
				if (string.IsNullOrWhiteSpace(name)) {
					error = dnSpy_Debugger_Resources.Exception_Error_NameCanNotBeEmpty;
					return false;
				}
				error = null;
				return true;
			}
		}

		public ICommand SaveCommand => new RelayCommand(a => Save(), a => CanSave);

		public bool IsVisible {
			get => isVisible;
			set {
				if (isVisible == value)
					return;
				isVisible = value;
				OnPropertyChanged(nameof(IsVisible));
				if (isVisible) {
					InitializeExceptionCategories();
					Reset();
				}
			}
		}
		bool isVisible;

		public object? SelectedCategory {
			get => selectedCategory;
			set {
				if (selectedCategory == value)
					return;
				selectedCategory = (ExceptionCategoryVM?)value;
				OnPropertyChanged(nameof(SelectedCategory));
				OnPropertyChanged(nameof(HasDescriptionText));
				nameOrCodeVM.IsCode = IsExceptionCode;
			}
		}
		ExceptionCategoryVM? selectedCategory;

		public object ExceptionCategoryCollection => exceptionCategories;
		readonly ObservableCollection<ExceptionCategoryVM> exceptionCategories;
		bool exceptionCategoriesInitd;

		public object NameCodeText => nameOrCodeVM;
		readonly NameOrCodeVM nameOrCodeVM;

		public string DescriptionTextToolTip => dnSpy_Debugger_Resources.ExceptionDescription;
		public string DescriptionText {
			get => descriptionText;
			set {
				if (descriptionText == value)
					return;
				descriptionText = value;
				OnPropertyChanged(nameof(DescriptionText));
			}
		}
		string descriptionText = string.Empty;

		bool IsExceptionCode => selectedCategory is not null && (selectedCategory.Definition.Flags & DbgExceptionCategoryDefinitionFlags.Code) != 0;
		public bool HasDescriptionText => IsExceptionCode;

		sealed class ExceptionCategoryVM : ViewModelBase {
			public string DisplayName => Definition.ShortDisplayName;
			public DbgExceptionCategoryDefinition Definition { get; }
			public ExceptionCategoryVM(DbgExceptionCategoryDefinition definition) => Definition = definition;
		}

		public AddExceptionVM(Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService) {
			exceptionCategories = new ObservableCollection<ExceptionCategoryVM>();
			this.dbgExceptionSettingsService = dbgExceptionSettingsService;
			nameOrCodeVM = new NameOrCodeVM(a => HasErrorUpdated());
		}

		void InitializeExceptionCategories() {
			if (exceptionCategoriesInitd)
				return;
			exceptionCategoriesInitd = true;
			foreach (var ex in dbgExceptionSettingsService.Value.CategoryDefinitions.Select(a => new ExceptionCategoryVM(a)).OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase))
				exceptionCategories.Add(ex);
			SelectedCategory = exceptionCategories.FirstOrDefault();
		}

		void Reset() {
			// Don't reset the selected category

			nameOrCodeVM.StringValue = string.Empty;
			DescriptionText = string.Empty;
		}

		bool CanSave => !HasError;
		void Save() {
			if (!CanSave)
				return;
			var id = CreateId();
			if (id is null)
				return;
			string? desc = descriptionText.Trim();
			if (desc == string.Empty)
				desc = null;
			var flags = DbgExceptionDefinitionFlags.StopFirstChance | DbgExceptionDefinitionFlags.StopSecondChance;
			var def = new DbgExceptionDefinition(id.Value, flags, desc);
			var settings = new DbgExceptionSettings(def.Flags);
			dbgExceptionSettingsService.Value.Add(new DbgExceptionSettingsInfo(def, settings));
			Reset();
		}

		DbgExceptionId? CreateId() {
			if (nameOrCodeVM.HasError)
				return null;
			if (selectedCategory is null)
				return null;
			if (IsExceptionCode) {
				if (!nameOrCodeVM.TryGetCode(out var code, out var error))
					return null;
				return new DbgExceptionId(selectedCategory.Definition.Name, code);
			}
			else {
				if (!nameOrCodeVM.TryGetName(out var name, out var error))
					return null;
				return new DbgExceptionId(selectedCategory.Definition.Name, name);
			}
		}

		public override bool HasError => nameOrCodeVM.HasError;
	}
}
