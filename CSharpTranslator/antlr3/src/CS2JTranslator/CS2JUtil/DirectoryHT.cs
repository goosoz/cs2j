/*
   Copyright 2010,2011 Kevin Glynn (kevin.glynn@twigletsoftware.com)
*/

using System;
using System.Text;
using System.Collections.Generic;

namespace Twiglet.CS2J.Translator.Utils
{

    // Implements a hierarchy of directories.
    public class DirectoryHT<TValue> : IDictionary<string, TValue>
    {

        private DirectoryHT<TValue> _parent = null;

        private Dictionary<string, TValue> leaves = new Dictionary<string, TValue>();

        private Dictionary<string, DirectoryHT<TValue>> children = new Dictionary<string, DirectoryHT<TValue>>();

        private IList<string> alts = new List<string>();

        public DirectoryHT(DirectoryHT<TValue> p)
        {
            _parent = p;
            alts.Add("");
        }

        public DirectoryHT()
            : this(null)
        { }


        public Dictionary<string, TValue> Leaves
        {
            get { return leaves; }
        }

        // p is key to a sub directory
        public DirectoryHT<TValue> Parent
        {  
            get { return _parent; }
        }

        // When searching for A.B.C.D.E we will first search for each A.B.C.D.<alt>.E where <alt> comes from Alts
        // This allows to override the default translations where necessary
        public IList<string> Alts
        {  
            get { return alts; }
        }

        // p is key to a sub directory
        public DirectoryHT<TValue> subDir(string p)
        {
            string[] components = p.Split(new char[] { '.' }, 2);
            if (components.Length == 1)
            {
                return children[components[0]];
            }
            else
            {
                DirectoryHT<TValue> child = children[components[0]];
                return (child == null ? null : child.subDir(components[1]));
            }
        }

        #region IDictionary Members

        public bool ContainsKey(string key)
        {
            string[] components = key.Split(new char[] { '.' }, 2);
            if (components.Length == 1)
            {
                return leaves.ContainsKey(components[0]);
            }
            else
            {
                if (children.ContainsKey(components[0]))
                {
                    return children[components[0]].ContainsKey(components[1]);
                }
                else
                {
                    return false;        
                }
            }
        }

        //        public IDictionaryEnumerator GetEnumerator()
        //        {
        //            IDictionaryEnumerator[] des = new IDictionaryEnumerator[1 + children.Count];
        //            string[] pres = new string[1 + children.Count];
        //            int i = 1;
        //
        //            pres[0] = "";
        //            des[0] = leaves.GetEnumerator();
        //            foreach (DictionaryEntry de in children)
        //            {
        //                pres[i] = ((string)de.Key) + ".";
        //                des[i] = ((DirectoryHT)de.Value).GetEnumerator();
        //                i++;
        //            }
        //
        //            return new DirectoryHTEnumerator(pres,des); 
        //        }

        public ICollection<string> Keys
        {
            get
            {
                List<string> keys = new List<string>();
                foreach (string k in leaves.Keys)
                    keys.Add(k);
                foreach (KeyValuePair<string, DirectoryHT<TValue>> de in children)
                    foreach (string k in de.Value.Keys)
                        keys.Add(de.Key + "." + k);
                return keys;
            }
        }

        public bool Remove(string key)
        {
            string[] components = ((string)key).Split(new char[] { '.' }, 2);
            if (components.Length == 1)
            {
                return leaves.Remove(components[0]);
            }
            else
            {
                return children[components[0]].Remove(components[1]);
            }
        }

        public ICollection<TValue> Values
        {

            get
            {
                List<TValue> vals = new List<TValue>();
                foreach (TValue v in leaves.Values)
                    vals.Add(v);
                foreach (KeyValuePair<string, DirectoryHT<TValue>> de in children)
                    foreach (TValue v in de.Value.Values)
                        vals.Add(v);
                return vals;
            }
        }

        public TValue this[string key]
        {
            get
            {
                // will throw KeyNotFound exception if not present
                string[] components = key.Split(new char[] { '.' }, 2);
                if (components.Length == 1)
                {
                    TValue val = leaves[key];
                    // keving: this isn't typesafe!: return (val != null ? val : children[components[0]]);
                    return val;
                }
                else
                {
                    DirectoryHT<TValue> child = children[components[0]];
                    return child[components[1]];
                }
            }
            set
            {
                Add(key, value);
            }
        }
        public bool TryGetValue(string key, out TValue value)
        {
            string[] components = key.Split(new char[] { '.' }, 2);
            if (components.Length == 1)
            {
                return leaves.TryGetValue(key, out value);
            }
            else
            {
                if (children.ContainsKey(components[0]))
                {
                    return children[components[0]].TryGetValue(components[1], out value);
                }
                else
                {
                    value = default(TValue);
                    return false;
                }
            }
        }

        // search for name, given searchPath
        // searchPath is searched in reverse order

        // When searching for A.B.C.D.E we will first search for each A.B.C.D.<alt>.E where <alt> comes from Alts
        // This allows to override the default translations where necessary
        public TValue Search(IList<string> searchPath, string name, TValue def) {
            
            // First check against each element of the search path 
            if (searchPath != null)
            {
               // check against each alt override in turn
               foreach  (string altIterator in Alts) {
                  string alt = altIterator.EndsWith(".")  ? altIterator : altIterator + ".";
               
                  for (int i = searchPath.Count-1; i >= 0; i--) {
                     String ns = searchPath[i];
                     String fullName = (ns ?? "") + (String.IsNullOrEmpty(ns) ? "" : ".") + name;
                     // insert alt after last period
                     int lastPeriod = fullName.LastIndexOf('.')+1;
                     fullName = fullName.Substring(0,lastPeriod) + alt + fullName.Substring(lastPeriod);
                    // Console.WriteLine(fullName);
                     if (this.ContainsKey(fullName)) {
                        return this[fullName];
                     }
                  }
               }

               // Not in alts, check kosher
               for (int i = searchPath.Count-1; i >= 0; i--) {
                  String ns = searchPath[i];
                  String fullName = (ns ?? "") + (String.IsNullOrEmpty(ns) ? "" : ".") + name;
                  // Console.WriteLine(fullName);
                  if (this.ContainsKey(fullName)) {
                     return this[fullName];
                  }
               }
            }

            // Check if name is fully qualified
            foreach  (string altIterator in Alts) {
               string alt = altIterator.EndsWith(".")  ? altIterator : altIterator + ".";
               // insert alt after last period
               int lastPeriod = name.LastIndexOf('.')+1;
               string fullName = name.Substring(0,lastPeriod) + alt + name.Substring(lastPeriod);
               // Console.WriteLine(fullName);
               if (this.ContainsKey(fullName)) {
                  return this[fullName];
               }
            }

            // Not in alt, check kosher
            // Console.WriteLine(name);
            if (this.ContainsKey(name)) {
               return this[name];
            }
            return def;
        }

        // search for name, given searchPath
        // searchPath is searched in reverse order
        public TValue Search(IList<string> searchPath, string name) {
            return Search(searchPath, name, default(TValue));
        }

        public TValue Search(string name) {
            return Search(new List<string>(), name);
        }

        public TValue Search(string name, TValue def) {
            return Search(new List<string>(), name, def);
        }

        public void Add(KeyValuePair<string, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<string, TValue> item)
        {
            TValue value;
            if (!this.TryGetValue(item.Key, out value))
                return false;

            return EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        public bool Remove(KeyValuePair<string, TValue> item)
        {
            if (!this.Contains(item))
                return false;

            return this.Remove(item.Key);
        }

        public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            Copy(this, array, arrayIndex);
        }

        public void Add(string key, TValue value)
        {
            string[] components = key.Split(new char[] { '.' }, 2);
            if (components.Length == 1)
            {
                leaves[components[0]] = value;
            }
            else
            {
                if (!children.ContainsKey(components[0]))
                    children[components[0]] = new DirectoryHT<TValue>(this);
                children[components[0]].Add(components[1], value);
            }
        }

        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            foreach (KeyValuePair<string, DirectoryHT<TValue>> de in children)
            {
                foreach (KeyValuePair<string, TValue> cur in de.Value)
                {
                    yield return new KeyValuePair<string, TValue>(de.Key + "." + cur.Key, cur.Value);
                }
            }
            foreach (KeyValuePair<string, TValue> de in leaves)
            {
                yield return new KeyValuePair<string, TValue>(de.Key, de.Value);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region ICollection Members

        public int Count
        {
            get
            {
                int count = leaves.Count;
                foreach (DirectoryHT<TValue> c in children.Values)
                    count += c.Count;
                return count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Clear()
        {
            leaves.Clear();
            children.Clear();
        }

        #endregion


        private static void Copy<T>(ICollection<T> source, T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");

            if ((array.Length - arrayIndex) < source.Count)
                throw new ArgumentException("Destination array is not large enough. Check array.Length and arrayIndex.");

            foreach (T item in source)
                array[arrayIndex++] = item;
        }
    }
}
