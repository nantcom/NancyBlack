using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using Newtonsoft.Json.Linq;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.CommerceSystem.types
{
    public enum PaymentUpdateReminderStatus
    {
        AutoAccepted = 0,
        Accepted = 1,
        Rejected = 2,
        Pending = 3,

        //FollowUp means there is remaining amount to be paid
        FollowUp = 4
    }

    public class PaymentUpdateReminder : IStaticType
    {
        public int Id { get; set; }

        public DateTime __createdAt { get; set; }

        public DateTime __updatedAt { get; set; }

        public int SaleOrderId { get; set; }

        public PaymentUpdateReminderStatus Status { get; private set; }

        public string Note { get; set; }

        public int HashAttachmentsText { get; private set; }

        public bool HasAttachmentBeenUpdated { get; private set; }

        public void UpdateHasAttachmentsString(SaleOrder so)
        {
            int currentHash = so.Attachments.GetHashCode();

            // do nothing if there is no update
            if (this.HashAttachmentsText != currentHash)
            {
                this.HasAttachmentBeenUpdated = true;
                this.HashAttachmentsText = currentHash;
                this.Status = PaymentUpdateReminderStatus.Pending;
            }
        }

        public void SetStatus(SaleOrder so, PaymentUpdateReminderStatus status)
        {
            this.HasAttachmentBeenUpdated = status == PaymentUpdateReminderStatus.Pending;
            this.Status = status;
            this.HashAttachmentsText = so.Attachments.GetHashCode();
        }
    }
}