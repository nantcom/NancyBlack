using Nancy.Bootstrapper;
using NantCom.NancyBlack.Configuration;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NantCom.NancyBlack
{
    public class GlobalVarEntry : IStaticType
    {
        public int Id { get; set; }
        public DateTime __createdAt { get; set; }
        public DateTime __updatedAt { get; set; }

        /// <summary>
        /// Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// A persistent variables that will keep in database
    /// </summary>
    public class GlobalVar : IPipelineHook
    {
        private static Dictionary<string, GlobalVarEntry> _Variables = new Dictionary<string, GlobalVarEntry>();
        private static HashSet<string> _DirtyList = new HashSet<string>();

        /// <summary>
        /// Gets/Set variable
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string key]
        {
            get
            {
                GlobalVarEntry gv;
                if (_Variables.TryGetValue(key, out gv))
                {
                    return gv.Value;
                }

                gv = new GlobalVarEntry() { Key = key, Value = null };
                _Variables[key] = gv;

                return null;
            }
            set
            {
                GlobalVarEntry gv;
                if (_Variables.TryGetValue(key, out gv))
                {
                    gv.Value = value;
                }
                else
                {
                    _Variables[key] = new GlobalVarEntry() { Key = key, Value = value };
                }

                _DirtyList.Add(key);
            }
        }

        private GlobalVar() { }

        /// <summary>
        /// Refreshes current global variable from database
        /// </summary>
        /// <param name="db"></param>
        public void Load(NancyBlackDatabase db)
        {
            lock ("GVLoad")
            {
                // persist current values first
                this.Persist(db);

                var list = db.Query<GlobalVarEntry>().OrderBy( gv => gv.Id ).ToList();
                var result = new Dictionary<string, GlobalVarEntry>();
                foreach (var item in list)
                {
                    result[item.Key] = item;
                }

                _Variables = result;
            }
        }

        /// <summary>
        /// Store value to database, only dirty values are stored
        /// </summary>
        /// <param name="db"></param>
        public void Persist(NancyBlackDatabase db)
        {
            lock (this)
            {
                db.Transaction(() =>
                {
                    foreach (var key in _DirtyList)
                    {
                        db.UpsertRecord(_Variables[key]);
                    }
                });

                _DirtyList = new HashSet<string>();
            }
        }

        public void Hook(IPipelines p)
        {
            p.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                this.Persist(ctx.GetSiteDatabase());
            });
        }

        private static GlobalVar _Default = new GlobalVar();

        /// <summary>
        /// Gets Default instance of Global Variable
        /// </summary>
        public static GlobalVar Default
        {
            get
            {
                return _Default;
            }
        }

    }
}