using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.DatabaseSystem.Types
{
    public class RowVersion : IStaticType
    {
        public int Id
        {
            get;
            set;
        }

        public DateTime __createdAt
        {
            get;
            set;
        }

        public DateTime __updatedAt
        {
            get;
            set;
        }

        public string __version
        {
            get;
            set;
        }

        /// <summary>
        /// User Host Address
        /// </summary>
        public string UserHostAddress { get; set; }

        /// <summary>
        /// Id of the row that was changed
        /// </summary>
        public int RowId { get; set; }

        /// <summary>
        /// Action performed on the row
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Name of the data type
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// JSON of the structure after the action has been made
        /// </summary>
        public string js_Row { get; set; }

        /// <summary>
        /// Users that made the action
        /// </summary>
        public int UserId { get; set; }
    }
}