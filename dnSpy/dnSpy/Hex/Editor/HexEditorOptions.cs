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
using System.Diagnostics.CodeAnalysis;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	sealed class HexEditorOptions : VSTE.IEditorOptions {
		public VSTE.IEditorOptions GlobalOptions => service.GlobalOptions;

		public VSTE.IEditorOptions? Parent {
			get => parent;
			set {
				// Check if we're the global options
				if (parent is null)
					throw new InvalidOperationException();
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				if (parent == value)
					return;
				var oldParent = parent;
				parent = (HexEditorOptions)value;
				UpdateOptions(oldParent);
			}
		}
		HexEditorOptions? parent;

		public IEnumerable<VSTE.EditorOptionDefinition> SupportedOptions {
			get {
				foreach (var def in service.EditorOptionDefinitions) {
					if (scope is null || def.IsApplicableToScope(scope))
						yield return def;
				}
			}
		}

		readonly Dictionary<string, object> dict;
		readonly HexEditorOptionsFactoryServiceImpl service;
		readonly List<WeakReference> weakChildren;
		readonly VSUTIL.IPropertyOwner? scope;

		public HexEditorOptions(HexEditorOptionsFactoryServiceImpl service, HexEditorOptions? parent, VSUTIL.IPropertyOwner? scope) {
			this.service = service;
			this.parent = parent;
			dict = new Dictionary<string, object>(StringComparer.Ordinal);
			weakChildren = new List<WeakReference>();
			this.scope = scope;
			UpdateOptions(null);
		}

		void UpdateOptions(HexEditorOptions? oldParent) {
			if (!(oldParent is null)) {
				for (int i = 0; i < oldParent.weakChildren.Count; i++) {
					if (oldParent.weakChildren[i].Target == this) {
						oldParent.weakChildren.RemoveAt(i);
						break;
					}
				}
			}
			if (!(parent is null))
				parent.weakChildren.Add(new WeakReference(this));

			if (!(parent is null) || !(oldParent is null)) {
				foreach (var o in SupportedOptions) {
					if (dict.ContainsKey(o.Name))
						continue;
					var oldValue = oldParent is null ? o.DefaultValue : oldParent.GetValueOrDefault(o.Name);
					var newValue = parent is null ? o.DefaultValue : parent.GetValueOrDefault(o.Name);
					if (!Equals(oldValue, newValue))
						OnChanged(o.Name);
				}
			}
		}

		bool TryGetValue(string optionId, [NotNullWhen(true)] out object? value) {
			if (!(scope is null) && !service.GetOption(optionId).IsApplicableToScope(scope))
				throw new InvalidOperationException();
			HexEditorOptions? p = this;
			while (!(p is null)) {
				if (p.dict.TryGetValue(optionId, out value))
					return true;
				p = p.parent;
			}
			value = null;
			return false;
		}

		object GetValueOrDefault(string optionId) {
			if (!TryGetValue(optionId, out var value))
				value = service.GetOption(optionId).DefaultValue;
			return value;
		}

		public event EventHandler<VSTE.EditorOptionChangedEventArgs>? OptionChanged;
		void OnChanged(string optionId) {
			if (scope is null || service.GetOption(optionId).IsApplicableToScope(scope))
				OptionChanged?.Invoke(this, new VSTE.EditorOptionChangedEventArgs(optionId));
			for (int i = weakChildren.Count - 1; i >= 0; i--) {
				if (!(weakChildren[i].Target is HexEditorOptions child)) {
					weakChildren.RemoveAt(i);
					continue;
				}
				if (!child.dict.ContainsKey(optionId))
					child.OnChanged(optionId);
			}
		}

		public bool IsOptionDefined<T>(VSTE.EditorOptionKey<T> key, bool localScopeOnly) => IsOptionDefined(key.Name, localScopeOnly);
		public bool IsOptionDefined(string optionId, bool localScopeOnly) {
			if (optionId is null)
				throw new ArgumentNullException(nameof(optionId));
			if (!(parent is null) && localScopeOnly)
				return dict.ContainsKey(optionId);
			var def = service.GetOption(optionId);
			return scope is null || def.IsApplicableToScope(scope);
		}

		public bool ClearOptionValue<T>(VSTE.EditorOptionKey<T> key) => ClearOptionValue(key.Name);
		public bool ClearOptionValue(string optionId) {
			if (optionId is null)
				throw new ArgumentNullException(nameof(optionId));
			if (parent is null || !dict.TryGetValue(optionId, out var oldValue))
				return false;
			dict.Remove(optionId);
			var newValue = GetValueOrDefault(optionId);
			if (!Equals(oldValue, newValue))
				OnChanged(optionId);
			return true;
		}

		public T GetOptionValue<T>(string optionId) => (T)GetOptionValue(optionId)!;
		public T GetOptionValue<T>(VSTE.EditorOptionKey<T> key) => (T)GetOptionValue(key.Name)!;
		public object? GetOptionValue(string optionId) {
			if (optionId is null)
				throw new ArgumentNullException(nameof(optionId));
			return GetValueOrDefault(optionId);
		}

		public void SetOptionValue<T>(VSTE.EditorOptionKey<T> key, T value) => SetOptionValue(key.Name, value);
		public void SetOptionValue(string optionId, object? value) {
			if (optionId is null)
				throw new ArgumentNullException(nameof(optionId));
			var def = service.GetOption(optionId);
			if (!(scope is null) && !def.IsApplicableToScope(scope))
				throw new InvalidOperationException();
			if (!def.IsValid(ref value))
				throw new ArgumentException();
			var oldValue = GetValueOrDefault(optionId);
			dict[optionId] = value;
			if (!Equals(oldValue, value))
				OnChanged(optionId);
		}
	}
}
