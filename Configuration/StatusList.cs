using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace NantCom.NancyBlack.Configuration
{
    public static class StatusList
    {
        public static List<string> GetAllStatus<T>()
        {
            return (typeof(T)
                    .GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.FieldType == typeof(string)).Select(f => (string)f.GetValue(null))).ToList();
        }
    }
}