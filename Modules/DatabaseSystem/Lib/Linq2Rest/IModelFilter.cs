// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IModelFilter.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the public interface for a model filter.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Linq2Rest
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.Contracts;
	using System.IO;
	using System.Linq;
	using System.Linq.Expressions;
	using Linq2Rest.Parser;

	/// <summary>
	/// Defines the public interface for a model filter.
	/// </summary>
	/// <typeparam name="T">The <see cref="Type"/> of item to filter.</typeparam>
	[ContractClass(typeof(ModelFilterContracts<>))]
	public interface IModelFilter<T>
	{
		/// <summary>
		/// Gets the filter expression.
		/// </summary>
		Expression<Func<T, bool>> FilterExpression { get; }

		/// <summary>
		/// Gets the amount of items to take.
		/// </summary>
		int TakeCount { get; }

		/// <summary>
		/// Gets the amount of items to skip.
		/// </summary>
		int SkipCount { get; }

		/// <summary>
		/// Gets the <see cref="SortDescription{T}"/> for the sequence.
		/// </summary>
		IEnumerable<SortDescription<T>> SortDescriptions { get; }
	}

	[ContractClassFor(typeof(IModelFilter<>))]
	internal abstract class ModelFilterContracts<T> : IModelFilter<T>
	{
		/// <summary>
		/// Gets the filter expression.
		/// </summary>
		public abstract Expression<Func<T, bool>> FilterExpression { get; }

		/// <summary>
		/// Gets the amount of items to take.
		/// </summary>
		public abstract int TakeCount { get; }

		/// <summary>
		/// Gets the amount of items to skip.
		/// </summary>
		public abstract int SkipCount { get; }

		/// <summary>
		/// Gets the <see cref="SortDescription{T}"/> for the sequence.
		/// </summary>
		public IEnumerable<SortDescription<T>> SortDescriptions
		{
			get
			{
				//Contract.Ensures(//Contract.Result<IEnumerable<SortDescription<T>>>() != null);

				throw new NotImplementedException();
			}
		}

		public IQueryable<object> Filter(IEnumerable<T> source)
		{
			//Contract.Requires<ArgumentNullException>(source != null);

			throw new NotImplementedException();
		}
	}
}