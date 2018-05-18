using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net
{
    /// <summary>
    /// Represents a collection that can be accessed either with the key or with the index. 
    /// </summary>
    public class KeyValueCollection<K,V> : IEnumerable
    {
        private Dictionary<K,V> m_pDictionary = null;
        private List<V>         m_pList       = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public KeyValueCollection()
        {
            m_pDictionary = new Dictionary<K,V>();
            m_pList = new List<V>();
        }


        #region method Add

        /// <summary>
        /// Adds the specified key and value to the collection.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void Add(K key,V value)
        {
            m_pDictionary.Add(key,value);
            m_pList.Add(value);
        }

        #endregion

        #region method Remove

        /// <summary>
        /// Removes the value with the specified key from the collection.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Returns if key found and removed, otherwise false.</returns>
        public bool Remove(K key)
        {
            V value = default(V);
            if(m_pDictionary.TryGetValue(key,out value)){
                m_pDictionary.Remove(key);
                m_pList.Remove(value);

                return true;
            }
            else{
                return false;
            }
        }

        #endregion

        #region method Clear

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            m_pDictionary.Clear();
            m_pList.Clear();
        }

        #endregion

        #region method ContainsKey

        /// <summary>
        /// Gets if the collection contains the specified key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Returns true if the collection contains specified key.</returns>
        public bool ContainsKey(K key)
        {
            return m_pDictionary.ContainsKey(key);
        }

        #endregion

        #region method TryGetValue

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found.</param>
        /// <returns>Returns true if the collection contains specified key and value stored to <b>value</b> argument.</returns>
        public bool TryGetValue(K key,out V value)
        {
            return m_pDictionary.TryGetValue(key,out value);
        }

        #endregion

        #region method TryGetValueAt

        /// <summary>
        /// Gets the value at the specified index.
        /// </summary>
        /// <param name="index">Zero based item index.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found.</param>
        /// <returns>Returns true if the collection contains specified key and value stored to <b>value</b> argument.</returns>
        public bool TryGetValueAt(int index,out V value)
        {
            value = default(V);

            if(m_pList.Count > 0 && index >= 0 && index < m_pList.Count){
                value = m_pList[index];

                return true;
            }

            return false;
        }

        #endregion

        #region method ToArray

        /// <summary>
        /// Copies all elements to new array, all elements will be in order they added. This method is thread-safe.
        /// </summary>
        /// <returns>Returns elements in a new array.</returns>
        public V[] ToArray()
        {
            lock(m_pList){
                return m_pList.ToArray();
            }
        }

        #endregion


        #region interface IEnumerator

        /// <summary>
		/// Gets enumerator.
		/// </summary>
		/// <returns>Returns IEnumerator interface.</returns>
		public IEnumerator GetEnumerator()
		{
			return m_pList.GetEnumerator();
		}

		#endregion

        #region Properties implementation

        /// <summary>
        /// Gets number of items int he collection.
        /// </summary>
        public int Count
        {
            get{ return m_pList.Count; }
        }

        /// <summary>
        /// Gets item with the specified key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Returns item with the specified key. If the specified key is not found, a get operation throws a KeyNotFoundException.</returns>
        public V this[K key]
        {
            get{ return m_pDictionary[key]; }
        }
   
        #endregion

    }
}
