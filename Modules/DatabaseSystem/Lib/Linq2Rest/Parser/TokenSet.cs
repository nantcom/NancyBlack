// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TokenSet.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the TokenSet type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Linq2Rest.Parser
{
	using System;
	using System.Diagnostics.Contracts;

	internal class TokenSet
	{
		private string _left;
		private string _operation;
		private string _right;

		public TokenSet()
		{
			_left = string.Empty;
			_right = string.Empty;
			_operation = string.Empty;
		}

		public string Left
		{
			get
			{
				//Contract.Ensures(//Contract.Result<string>() != null);
				return _left;
			}

			set
			{
				//Contract.Requires<ArgumentNullException>(value != null);
				_left = value;
			}
		}

		public string Operation
		{
			get
			{
				//Contract.Ensures(//Contract.Result<string>() != null);
				return _operation;
			}

			set
			{
				//Contract.Requires<ArgumentNullException>(value != null);
				_operation = value;
			}
		}

		public string Right
		{
			get
			{
				//Contract.Ensures(//Contract.Result<string>() != null);
				return _right;
			}

			set
			{
				//Contract.Requires<ArgumentNullException>(value != null);
				_right = value;
			}
		}

		public override string ToString()
		{
			return string.Format("{0} {1} {2}", Left, Operation, Right);
		}

		[ContractInvariantMethod]
		private void Invariants()
		{
			//Contract.Invariant(_left != null);
			//Contract.Invariant(_right != null);
			//Contract.Invariant(_operation != null);
		}
	}
}