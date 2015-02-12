﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class TempDataDictionary : IDictionary<string, object>
    {
        private Dictionary<string, object> _data;
        private bool _loaded;
        private ITempDataProvider _provider;
        private ActionContext _context;
        private HashSet<string> _initialKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _retainedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public TempDataDictionary(ActionContext context, ITempDataProvider provider)
        {
            _provider = provider;
            _loaded = false;
            _context = context;
            _data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public ICollection<string> Keys => _data.Keys;

        public ICollection<object> Values => _data.Values;

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return ((ICollection<KeyValuePair<string, object>>)_data).IsReadOnly; }
        }

        public object this[string key]
        {
            get
            {
                Load();
                object value;
                if (TryGetValue(key, out value))
                {
                    _initialKeys.Remove(key);
                    return value;
                }
                return null;
            }
            set
            {
                Load();
                _data[key] = value;
                _initialKeys.Add(key);
            }
        }

        public void Keep()
        {
            _retainedKeys.Clear();
            _retainedKeys.UnionWith(_data.Keys);
        }

        public void Keep(string key)
        {
            _retainedKeys.Add(key);
        }

        public void Load()
        {
            if (_loaded)
            {
                return;
            }

            var providerDictionary = _provider.LoadTempData(_context);
            _data = (providerDictionary != null)
                ? new Dictionary<string, object>(providerDictionary, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _initialKeys = new HashSet<string>(_data.Keys, StringComparer.OrdinalIgnoreCase);
            _retainedKeys.Clear();
            _loaded = true;
        }

        public object Peek(string key)
        {
            object value;
            _data.TryGetValue(key, out value);
            return value;
        }

        public void Save()
        {
            if (!_loaded)
            {
                return;
            }

            _data.RemoveFromDictionary((KeyValuePair<string, object> entry, TempDataDictionary tempData) =>
            {
                var key = entry.Key;
                return !tempData._initialKeys.Contains(key)
                    && !tempData._retainedKeys.Contains(key);
            }, this);

            _provider.SaveTempData(_context, _data);
        }

        public void Add(string key, object value)
        {
            _data.Add(key, value);
            _initialKeys.Add(key);
        }

        public void Clear()
        {
            _data.Clear();
            _retainedKeys.Clear();
            _initialKeys.Clear();
        }

        public bool ContainsKey(string key)
        {
            return _data.ContainsKey(key);
        }

        public bool ContainsValue(object value)
        {
            return _data.ContainsValue(value);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return new TempDataDictionaryEnumerator(this);
        }

        public bool Remove(string key)
        {
            _retainedKeys.Remove(key);
            _initialKeys.Remove(key);
            return _data.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            _initialKeys.Remove(key);
            return _data.TryGetValue(key, out value);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int index)
        {
            ((ICollection<KeyValuePair<string, object>>)_data).CopyTo(array, index);
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> keyValuePair)
        {
            _initialKeys.Add(keyValuePair.Key);
            ((ICollection<KeyValuePair<string, object>>)_data).Add(keyValuePair);
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> keyValuePair)
        {
            return ((ICollection<KeyValuePair<string, object>>)_data).Contains(keyValuePair);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> keyValuePair)
        {
            _initialKeys.Remove(keyValuePair.Key);
            return ((ICollection<KeyValuePair<string, object>>)_data).Remove(keyValuePair);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new TempDataDictionaryEnumerator(this);
        }

        private sealed class TempDataDictionaryEnumerator : IEnumerator<KeyValuePair<string, object>>
        {
            private IEnumerator<KeyValuePair<string, object>> _enumerator;
            private TempDataDictionary _tempData;

            public TempDataDictionaryEnumerator(TempDataDictionary tempData)
            {
                _tempData = tempData;
                _enumerator = _tempData._data.GetEnumerator();
            }

            public KeyValuePair<string, object> Current
            {
                get
                {
                    var kvp = _enumerator.Current;
                    _tempData._initialKeys.Remove(kvp.Key);
                    return kvp;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }

            void IDisposable.Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }
}