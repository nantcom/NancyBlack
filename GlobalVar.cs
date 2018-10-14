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
    public class GlobalVar
    {
        private static Dictionary<string, GlobalVarEntry> _Variables;
        private static List<string> _DirtyList = new List<string>();

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
        /// Load variable from database, this will only load once per application start up
        /// </summary>
        /// <param name="db"></param>
        public void Load(NancyBlackDatabase db)
        {
            if (_Variables != null)
            {
                return;
            }

            _Variables = db.Query<GlobalVarEntry>().ToDictionary(gv => gv.Key, gv => gv);
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

                _DirtyList = new List<string>();
            }
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