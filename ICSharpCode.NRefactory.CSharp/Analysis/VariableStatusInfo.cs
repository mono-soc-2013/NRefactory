// 
// VariableStatusInfo.cs
//  
// Author:
//       Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Text;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	sealed class VariableStatusInfo : IEquatable<VariableStatusInfo>, IEnumerable<KeyValuePair<string, NullValueStatus>>
	{
		readonly Dictionary<string, NullValueStatus> VariableStatus = new Dictionary<string, NullValueStatus>();
		
		public NullValueStatus this[string name]
		{
			get {
				NullValueStatus status;
				if (VariableStatus.TryGetValue(name, out status)) {
					return status;
				}
				return NullValueStatus.UnreachableOrInexistent;
			}
			set {
				if (value == NullValueStatus.UnreachableOrInexistent) {
					VariableStatus.Remove(name);
				} else {
					VariableStatus [name] = value;
				}
			}
		}
		
		/// <summary>
		/// Modifies the variable state to consider a new incoming path
		/// </summary>
		/// <returns><c>true</c>, if the state has changed, <c>false</c> otherwise.</returns>
		/// <param name="incomingState">The variable state of the incoming path</param>
		public bool ReceiveIncoming(VariableStatusInfo incomingState)
		{
			bool changed = false;
			var listOfVariables = VariableStatus.Keys.Concat(incomingState.VariableStatus.Keys).ToList();
			foreach (string variable in listOfVariables)
			{
				var newValue = CombineStatus(this [variable], incomingState [variable]);
				if (this [variable] != newValue) {
					this [variable] = newValue;
					changed = true;
				}
			}
			
			return changed;
		}
		
		public static NullValueStatus CombineStatus(NullValueStatus oldValue, NullValueStatus incomingValue)
		{
			if (oldValue == NullValueStatus.Error || incomingValue == NullValueStatus.Error)
				return NullValueStatus.Error;
			
			if (oldValue == NullValueStatus.UnreachableOrInexistent ||
			   oldValue == NullValueStatus.Unassigned)
				return incomingValue;
			
			if (incomingValue == NullValueStatus.Unassigned) {
				return NullValueStatus.Unassigned;
			}
			
			if (oldValue == NullValueStatus.CapturedUnknown || incomingValue == NullValueStatus.CapturedUnknown) {
				//TODO: Check if this is right
				return NullValueStatus.CapturedUnknown;
			}
			
			if (oldValue == NullValueStatus.Unknown) {
				return NullValueStatus.Unknown;
			}
			
			if (oldValue == NullValueStatus.DefinitelyNull) {
				return incomingValue == NullValueStatus.DefinitelyNull ?
					   NullValueStatus.DefinitelyNull : NullValueStatus.PotentiallyNull;
			}
			
			if (oldValue == NullValueStatus.DefinitelyNotNull) {
				if (incomingValue == NullValueStatus.Unknown)
					return NullValueStatus.Unknown;
				if (incomingValue == NullValueStatus.DefinitelyNotNull)
					return NullValueStatus.DefinitelyNotNull;
				return NullValueStatus.PotentiallyNull;
			}
			
			Debug.Assert(oldValue == NullValueStatus.PotentiallyNull);
			return NullValueStatus.PotentiallyNull;
		}
		
		public bool HasVariable(string variable) {
			return VariableStatus.ContainsKey(variable);
		}
		
		public VariableStatusInfo Clone() {
			var clone = new VariableStatusInfo();
			foreach (var item in VariableStatus) {
				clone.VariableStatus.Add(item.Key, item.Value);
			}
			return clone;
		}
		
		public override bool Equals(object obj)
		{
			return Equals(obj as VariableStatusInfo);
		}
		
		public bool Equals(VariableStatusInfo obj)
		{
			if (obj == null) {
				return false;
			}
			
			if (VariableStatus.Count != obj.VariableStatus.Count)
				return false;
			
			return VariableStatus.All(item => item.Value == obj[item.Key]);
		}
		
		public override int GetHashCode()
		{
			//STUB
			return VariableStatus.Count.GetHashCode();
		}
		
		public static bool operator ==(VariableStatusInfo obj1, VariableStatusInfo obj2) {
			return object.ReferenceEquals(obj1, null) ?
				object.ReferenceEquals(obj2, null) : obj1.Equals(obj2);
		}
		
		public static bool operator !=(VariableStatusInfo obj1, VariableStatusInfo obj2) {
			return !(obj1 == obj2);
		}
		
		public IEnumerator<KeyValuePair<string, NullValueStatus>> GetEnumerator()
		{
			return VariableStatus.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		public override string ToString()
		{
			var builder = new StringBuilder("[");
			foreach (var item in this) {
				builder.Append(item.Key);
				builder.Append("=");
				builder.Append(item.Value);
			}
			builder.Append("]");
			return builder.ToString();
		}
	}
}
