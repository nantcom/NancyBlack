using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules
{
    public abstract class BaseModule : NancyModule
    {

        /// <summary>
        /// Handles the static request.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        protected Func<dynamic, dynamic> HandleStaticRequest(string view, dynamic model)
        {
            return (arg) =>
            {
                return View[view, model];
            };
        }


        /// <summary>
        /// Handles the request.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        protected Func<dynamic, dynamic> HandleRequest(Func<dynamic, dynamic> action)
        {
            return (arg) =>
            {
                dynamic result = null;
                try
                {
                    result = action(arg);
                }
                catch (InvalidOperationException ex)
                {
                    this.Context.Items["Exception"] = ex;

                    return this.Negotiate
                        .WithStatusCode(400)
                        .WithModel(new
                        {
                            Code = 400,
                            ExceptionType = ex.GetType().Name,
                            Message = ex.Message
                        });
                }
                catch (Exception ex)
                {
                    this.Context.Items["Exception"] = ex;

                    return this.Negotiate
                        .WithStatusCode(500)
                        .WithModel(new
                        {
                            Code = 500,
                            ExceptionType = ex.GetType().Name,
                            Message = ex.Message
                        });
                }

                return result;
            };
        }

    }
}