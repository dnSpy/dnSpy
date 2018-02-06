/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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

using System.Diagnostics.Tracing;

namespace dnSpy.Contracts.ETW {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
	[EventSource(Name = "dnSpy")]
	public sealed class DnSpyEventSource : EventSource {
		public static readonly DnSpyEventSource Log = new DnSpyEventSource();
		DnSpyEventSource() { }

		[Event(1)]
		public void StartupStart() => WriteEvent(1);
		[Event(2)]
		public void StartupStop() => WriteEvent(2);

		[Event(3)]
		public void SaveDocumentsStart() => WriteEvent(3);
		[Event(4)]
		public void SaveDocumentsStop() => WriteEvent(4);

		[Event(5)]
		public void CompileStart() => WriteEvent(5);
		[Event(6)]
		public void CompileStop() => WriteEvent(6);

		[Event(7)]
		public void ExportToProjectStart() => WriteEvent(7);
		[Event(8)]
		public void ExportToProjectStop() => WriteEvent(8);

		[Event(9)]
		public void OpenFromGACStart() => WriteEvent(9);
		[Event(10)]
		public void OpenFromGACStop() => WriteEvent(10);

		[Event(11)]
		public void ShowDocumentTabContentStart() => WriteEvent(11);
		[Event(12)]
		public void ShowDocumentTabContentStop() => WriteEvent(12);
	}
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
}
