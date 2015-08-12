// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MethodProvider.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the MethodProvider type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Linq2Rest
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.Contracts;
	using System.Linq;
	using System.Reflection;

	internal static class MethodProvider
	{
		private static readonly MethodInfo InnerContainsMethod;
		private static readonly MethodInfo InnerIndexOfMethod;
		private static readonly MethodInfo EndsWithMethod1;
		private static readonly MethodInfo InnerStartsWithMethod;
		private static readonly PropertyInfo InnerLengthProperty;
		private static readonly MethodInfo InnerSubstringMethod;
		private static readonly MethodInfo InnerToLowerMethod;
		private static readonly MethodInfo InnerToUpperMethod;
		private static readonly MethodInfo InnerTrimMethod;
		private static readonly PropertyInfo InnerDayProperty;
		private static readonly PropertyInfo InnerHourProperty;
		private static readonly PropertyInfo InnerMinuteProperty;
		private static readonly PropertyInfo InnerSecondProperty;
		private static readonly PropertyInfo InnerMonthProperty;
		private static readonly PropertyInfo InnerYearProperty;
		private static readonly MethodInfo InnerDoubleRoundMethod;
		private static readonly MethodInfo InnerDecimalRoundMethod;
		private static readonly MethodInfo InnerDoubleFloorMethod;
		private static readonly MethodInfo InnerDecimalFloorMethod;
		private static readonly MethodInfo InnerDoubleCeilingMethod;
		private static readonly MethodInfo InnerDecimalCeilingMethod;

		static MethodProvider()
		{
			var stringType = typeof(string);
			var datetimeType = typeof(DateTime);
			var mathType = typeof(Math);
			
			InnerContainsMethod = stringType.GetMethod("Contains", new[] { stringType });
			InnerIndexOfMethod = stringType.GetMethod("IndexOf", new[] { stringType });
			EndsWithMethod1 = stringType.GetMethod("EndsWith", new[] { stringType });
			InnerStartsWithMethod = stringType.GetMethod("StartsWith", new[] { stringType });
			InnerLengthProperty = stringType.GetProperty("Length", Type.EmptyTypes);
			InnerSubstringMethod = stringType.GetMethod("Substring", new[] { typeof(int) });
			InnerToLowerMethod = stringType.GetMethod("ToLowerInvariant", Type.EmptyTypes);
			InnerToUpperMethod = stringType.GetMethod("ToUpperInvariant", Type.EmptyTypes);
			InnerTrimMethod = stringType.GetMethod("Trim", Type.EmptyTypes);

			InnerDayProperty = datetimeType.GetProperty("Day", Type.EmptyTypes);
			InnerHourProperty = datetimeType.GetProperty("Hour", Type.EmptyTypes);
			InnerMinuteProperty = datetimeType.GetProperty("Minute", Type.EmptyTypes);
			InnerSecondProperty = datetimeType.GetProperty("Second", Type.EmptyTypes);
			InnerMonthProperty = datetimeType.GetProperty("Month", Type.EmptyTypes);
			InnerYearProperty = datetimeType.GetProperty("Year", Type.EmptyTypes);

			InnerDoubleRoundMethod = mathType.GetMethod("Round", new[] { typeof(double) });
			InnerDecimalRoundMethod = mathType.GetMethod("Round", new[] { typeof(decimal) });
			InnerDoubleFloorMethod = mathType.GetMethod("Floor", new[] { typeof(double) });
			InnerDecimalFloorMethod = mathType.GetMethod("Floor", new[] { typeof(decimal) });
			InnerDoubleCeilingMethod = mathType.GetMethod("Ceiling", new[] { typeof(double) });
			InnerDecimalCeilingMethod = mathType.GetMethod("Ceiling", new[] { typeof(decimal) });
		}

		public static MethodInfo IndexOfMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerIndexOfMethod;
			}
		}

		public static MethodInfo ContainsMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerContainsMethod;
			}
		}

		public static MethodInfo EndsWithMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return EndsWithMethod1;
			}
		}

		public static MethodInfo StartsWithMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerStartsWithMethod;
			}
		}

		public static PropertyInfo LengthProperty
		{
			get
			{
				//Contract.Ensures(//Contract.Result<PropertyInfo>() != null);
				return InnerLengthProperty;
			}
		}

		public static MethodInfo SubstringMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerSubstringMethod;
			}
		}

		public static MethodInfo ToLowerMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerToLowerMethod;
			}
		}

		public static MethodInfo ToUpperMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerToUpperMethod;
			}
		}

		public static MethodInfo TrimMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerTrimMethod;
			}
		}

		public static PropertyInfo DayProperty
		{
			get
			{
				//Contract.Ensures(//Contract.Result<PropertyInfo>() != null);
				return InnerDayProperty;
			}
		}

		public static PropertyInfo HourProperty
		{
			get
			{
				//Contract.Ensures(//Contract.Result<PropertyInfo>() != null);
				return InnerHourProperty;
			}
		}

		public static PropertyInfo MinuteProperty
		{
			get
			{
				//Contract.Ensures(//Contract.Result<PropertyInfo>() != null);
				return InnerMinuteProperty;
			}
		}

		public static PropertyInfo SecondProperty
		{
			get
			{
				//Contract.Ensures(//Contract.Result<PropertyInfo>() != null);
				return InnerSecondProperty;
			}
		}

		public static PropertyInfo MonthProperty
		{
			get
			{
				//Contract.Ensures(//Contract.Result<PropertyInfo>() != null);
				return InnerMonthProperty;
			}
		}

		public static PropertyInfo YearProperty
		{
			get
			{
				//Contract.Ensures(//Contract.Result<PropertyInfo>() != null);
				return InnerYearProperty;
			}
		}

		public static MethodInfo DoubleRoundMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerDoubleRoundMethod;
			}
		}

		public static MethodInfo DecimalRoundMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerDecimalRoundMethod;
			}
		}

		public static MethodInfo DoubleFloorMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerDoubleFloorMethod;
			}
		}

		public static MethodInfo DecimalFloorMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerDecimalFloorMethod;
			}
		}

		public static MethodInfo DoubleCeilingMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerDoubleCeilingMethod;
			}
		}

		public static MethodInfo DecimalCeilingMethod
		{
			get
			{
				//Contract.Ensures(//Contract.Result<MethodInfo>() != null);
				return InnerDecimalCeilingMethod;
			}
		}

		public static MethodInfo GetAnyAllMethod(string name, Type collectionType)
		{
			//Contract.Requires(collectionType != null);

			var implementationType = GetIEnumerableImpl(collectionType);

			var elemType = implementationType.GetGenericArguments()[0];
			var predType = typeof(Func<,>).MakeGenericType(elemType, typeof(bool));

			var allMethod = (MethodInfo)GetGenericMethod(
														 typeof(Enumerable), 
														 name, 
														 new[] { elemType }, 
														 new[] { implementationType, predType }, 
														 BindingFlags.Static);

			return allMethod;
		}

		public static Type GetIEnumerableImpl(Type type)
		{
			//Contract.Requires(type != null);

			// Get IEnumerable implementation. Either type is IEnumerable<T> for some T, 
			// or it implements IEnumerable<T> for some T. We need to find the interface.
			if (IsIEnumerable(type))
			{
				return type;
			}

			var interfaces = type.FindInterfaces((m, o) => IsIEnumerable(m), null);

			//Contract.Assume(interfaces.Count() > 0);

			var t = interfaces.First();

			return t;
		}

		private static MethodBase GetGenericMethod(Type type, string name, Type[] typeArgs, Type[] argTypes, BindingFlags flags)
		{
			//Contract.Requires(typeArgs != null);
			//Contract.Requires(type != null);
			//Contract.Assume(Type.DefaultBinder != null);

			var typeArity = typeArgs.Length;
			var methods = type.GetMethods()
				.Where(m => m.Name == name)
				.Where(m => m.GetGenericArguments().Length == typeArity)
				.Select(m => m.MakeGenericMethod(typeArgs));

			return Type.DefaultBinder.SelectMethod(flags, methods.ToArray(), argTypes, null);
		}

		private static bool IsIEnumerable(Type type)
		{
			//Contract.Requires(type != null);

			return type.IsGenericType
				&& type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
		}
	}
}