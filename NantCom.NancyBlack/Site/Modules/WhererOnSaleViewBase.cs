using NantCom.NancyBlack.Site.Modules.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Site.Modules
{
    public abstract class WhererOnSaleViewBase : NancyBlackRazorViewBase
    {
        public IEnumerable<Categories> GetCategories(WhereOnSaleContent content)
        {
            if (content.CatsId == null || content.CatsId.Count() == 0)
            {
                yield break;
            }

            foreach (var catId in content.CatsId)
            {
                yield return this.Database.GetById<Categories>(catId);
            }
        }
    }
}