using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net
{
    /// <summary>
    /// Implements byte data builder.
    /// </summary>
    public class ByteBuilder
    {
        private int      m_BlockSize = 1024;
        private byte[]   m_pBuffer   = null;
        private int      m_Count     = 0;
        private Encoding m_pCharset  = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ByteBuilder()
        {
            m_pBuffer  = new byte[m_BlockSize];
            m_pCharset = Encoding.UTF8;
        }


        #region method Append

        /// <summary>
        /// Appends specified string value to the buffer. String is encoded with <see cref="Charset"/>.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <exception cref="ArgumentNullException">Is aised when <b>value</b> is null reference.</exception>
        public void Append(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            Append(m_pCharset.GetBytes(value));
        }

        /// <summary>
        /// Appends specified string value to the buffer.
        /// </summary>
        /// <param name="charset">Character encoding.</param>
        /// <param name="value">String value.</param>
        /// <exception cref="ArgumentNullException">Is aised when <b>charset</b> or <b>value</b> is null reference.</exception>
        public void Append(Encoding charset,string value)
        {
            if(charset == null){
                throw new ArgumentNullException("charset");
            }
            if(value == null){
                throw new ArgumentNullException("value");
            }

            Append(charset.GetBytes(value));
        }

        /// <summary>
        /// Appends specified byte[] value to the buffer.
        /// </summary>
        /// <param name="value">Byte value.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        public void Append(byte[] value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            Append(value,0,value.Length);
        }

        /// <summary>
        /// Appends specified byte[] value to the buffer.
        /// </summary>
        /// <param name="value">Byte value.</param>
        /// <param name="offset">Offset in the value.</param>
        /// <param name="count">Number of bytes to append.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        public void Append(byte[] value,int offset,int count)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            // Increase buffer if needed.
            while((m_pBuffer.Length - m_Count) < count){
                byte[] newBuffer = new byte[m_pBuffer.Length + m_BlockSize];
                Array.Copy(m_pBuffer,newBuffer,m_Count);
                m_pBuffer = newBuffer;
            }

            Array.Copy(value,offset,m_pBuffer,m_Count,count);
            m_Count += value.Length;
        }

        #endregion

        #region method ToByte

        /// <summary>
        /// Returns this as byte[] data.
        /// </summary>
        /// <returns>Returns this as byte[] data.</returns>
        public byte[] ToByte()
        {
            byte[] retVal = new byte[m_Count];
            Array.Copy(m_pBuffer,retVal,m_Count);

            return retVal;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets number of bytes in byte builder buffer.
        /// </summary>
        public int Count
        {
            get{ return m_Count; }
        }

        /// <summary>
        /// Gets or sets default charset encoding used for string related operations.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null reference value is set.</exception>
        public Encoding Charset
        {
            get{ return m_pCharset; }

            set{
                if(value == null){
                    throw new ArgumentNullException("value");
                }

                m_pCharset = value;
            }
        }

        #endregion

    }
}
