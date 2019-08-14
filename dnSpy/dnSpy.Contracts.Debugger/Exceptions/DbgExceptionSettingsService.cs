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

namespace dnSpy.Contracts.Debugger.Exceptions {
	/// <summary>
	/// Exception settings service
	/// </summary>
	public abstract class DbgExceptionSettingsService {
		/// <summary>
		/// Resets all exception settings and removes user-added exceptions
		/// </summary>
		public abstract void Reset();

		/// <summary>
		/// Modifies an existing exception. It raises <see cref="ExceptionSettingsModified"/> if the
		/// new settings are not equal to the current settings.
		/// </summary>
		/// <param name="id">Id of existing exception</param>
		/// <param name="settings">New settings</param>
		public void Modify(DbgExceptionId id, DbgExceptionSettings settings) =>
			Modify(new[] { new DbgExceptionIdAndSettings(id, settings) });

		/// <summary>
		/// Modifies existing exceptions. It raises <see cref="ExceptionSettingsModified"/> if the
		/// new settings are not equal to the current settings.
		/// </summary>
		/// <param name="settings">New settings</param>
		public abstract void Modify(DbgExceptionIdAndSettings[] settings);

		/// <summary>
		/// Raised when an exception is modified
		/// </summary>
		public abstract event EventHandler<DbgExceptionSettingsModifiedEventArgs>? ExceptionSettingsModified;

		/// <summary>
		/// Removes exception settings
		/// </summary>
		/// <param name="ids">IDs of all exceptions to remove</param>
		public abstract void Remove(DbgExceptionId[] ids);

		/// <summary>
		/// Adds an exception
		/// </summary>
		/// <param name="settings">Exception settings</param>
		public void Add(in DbgExceptionSettingsInfo settings) => Add(new[] { settings });

		/// <summary>
		/// Adds exceptions
		/// </summary>
		/// <param name="settings">Exception settings</param>
		public abstract void Add(DbgExceptionSettingsInfo[] settings);

		/// <summary>
		/// Gets all exceptions
		/// </summary>
		public abstract DbgExceptionSettingsInfo[] Exceptions { get; }

		/// <summary>
		/// Raised when <see cref="Exceptions"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgExceptionSettingsInfo>>? ExceptionsChanged;

		/// <summary>
		/// Returns the exception definition if it exists
		/// </summary>
		/// <param name="id">Exception id</param>
		/// <param name="definition">Updated with the exception definition if the method returns true</param>
		/// <returns></returns>
		public abstract bool TryGetDefinition(DbgExceptionId id, out DbgExceptionDefinition definition);

		/// <summary>
		/// Returns exception settings or false if the exception doesn't exist in the collection
		/// </summary>
		/// <param name="id">Id of exception</param>
		/// <param name="settings">Updated with the exception settings if the method returns true</param>
		/// <returns></returns>
		public abstract bool TryGetSettings(DbgExceptionId id, out DbgExceptionSettings settings);

		/// <summary>
		/// Returns exception settings. If the exception doesn't exist in the collection, the default exception settings is returned
		/// </summary>
		/// <param name="id">Id of exception</param>
		/// <returns></returns>
		public abstract DbgExceptionSettings GetSettings(DbgExceptionId id);

		/// <summary>
		/// Gets the category definition if it exists
		/// </summary>
		/// <param name="category">Category, see <see cref="PredefinedExceptionCategories"/></param>
		/// <param name="definition">Updated with the category definition if the method returns true</param>
		/// <returns></returns>
		public abstract bool TryGetCategoryDefinition(string category, out DbgExceptionCategoryDefinition definition);

		/// <summary>
		/// Gets all category definitions
		/// </summary>
		public abstract ReadOnlyCollection<DbgExceptionCategoryDefinition> CategoryDefinitions { get; }
	}

	/// <summary>
	/// Contains the exception definition and exception settings
	/// </summary>
	public readonly struct DbgExceptionSettingsInfo {
		/// <summary>
		/// Gets the definition
		/// </summary>
		public DbgExceptionDefinition Definition { get; }

		/// <summary>
		/// Gets the settings
		/// </summary>
		public DbgExceptionSettings Settings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="definition">Exception definition</param>
		/// <param name="settings">Exception settings</param>
		public DbgExceptionSettingsInfo(DbgExceptionDefinition definition, DbgExceptionSettings settings) {
			if (definition.Id.Category is null)
				throw new ArgumentException();
			if (settings.Conditions is null)
				throw new ArgumentException();
			Definition = definition;
			Settings = settings;
		}
	}

	/// <summary>
	/// Exception id and settings
	/// </summary>
	public readonly struct DbgExceptionIdAndSettings {
		/// <summary>
		/// Gets the exception id
		/// </summary>
		public DbgExceptionId Id { get; }

		/// <summary>
		/// Gets the settings
		/// </summary>
		public DbgExceptionSettings Settings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="id">Exception id</param>
		/// <param name="settings">Settings</param>
		public DbgExceptionIdAndSettings(DbgExceptionId id, DbgExceptionSettings settings) {
			if (id.Category is null)
				throw new ArgumentException();
			if (settings.Conditions is null)
				throw new ArgumentException();
			Id = id;
			Settings = settings;
		}
	}

	/// <summary>
	/// <see cref="DbgExceptionSettingsService.ExceptionSettingsModified"/> event args
	/// </summary>
	public readonly struct DbgExceptionSettingsModifiedEventArgs {
		/// <summary>
		/// Gets the ID and new settings
		/// </summary>
		public ReadOnlyCollection<DbgExceptionIdAndSettings> IdAndSettings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="idAndSettings">New settings</param>
		public DbgExceptionSettingsModifiedEventArgs(ReadOnlyCollection<DbgExceptionIdAndSettings> idAndSettings) =>
			IdAndSettings = idAndSettings ?? throw new ArgumentNullException(nameof(idAndSettings));
	}
}
