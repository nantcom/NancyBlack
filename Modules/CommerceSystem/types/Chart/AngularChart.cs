using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types.Chart
{
    public enum AngularChartType
    {
        Line,
        Bar,
        Pie,
        Radar,
        PolarArea
    }

    public class AngularChart
    {
        public string Title { get; set; }

        public AngularChartType EnumType { get; set; }

        public string Type
        {
            get
            {
                return this.EnumType.ToString();
            }
        }

        public bool IsLegend { get; set; }

        public List<string> Labels { get; set; }

        public List<string> Series { get; set; }

        public List<dynamic> Data { get; set; }

        public List<string> Colours { get; set; }
    }
}