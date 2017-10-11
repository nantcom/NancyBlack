﻿using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.MailingListSystem
{
    public class NcbMailingListSubscription : IStaticType
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

        /// <summary>
        /// First name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Last Name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Birth Day
        /// </summary>
        public string BirthDay { get; set; }

        /// <summary>
        /// Whether this subscription is still active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The affiliate code which refers to this registration
        /// </summary>
        public string RefererAffiliateCode { get; set; }

    }
}