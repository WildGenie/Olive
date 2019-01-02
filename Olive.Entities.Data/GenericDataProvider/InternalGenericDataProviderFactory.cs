﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Olive.Entities.Data
{
    internal static class InternalDataProviderFactory
    {
        static Dictionary<Type, DataProvider> Cache =
            new Dictionary<Type, DataProvider>();


        public static DataProvider Get(Type type, ICache cache, IDataAccess access, ISqlCommandGenerator sqlCommandGenerator)
        {
            lock (Cache)
            {
                if (Cache.ContainsKey(type)) return Cache[type];

                var result = new DataProvider(type, cache, access, sqlCommandGenerator);
                Cache.Add(type, result);

                return result;
            }
        }
    }
}