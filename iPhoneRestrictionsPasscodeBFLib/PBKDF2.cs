using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace iPhoneRestrictionsPasscodeBFLib
{
    /// <summary>
    /// It is a clone of Rfc2898DeriveBytes class, without the restriction of salt length (minimun of 8 bytes)
    /// </summary>
    public class PBKDF2 : DeriveBytes
    {
        private byte[] m_buffer;
        private byte[] m_salt;
        private HMACSHA1 m_hmacsha1;  // The pseudo-random generator function used in PBKDF2

        private uint m_iterations;
        private uint m_block;
        private int m_startIndex;
        private int m_endIndex;

        private const int BlockSize = 20;

        public PBKDF2(string password, byte[] salt) : this(password, salt, 1000) { }

        public PBKDF2(string password, byte[] salt, int iterations) : this(new UTF8Encoding(false).GetBytes(password), salt, iterations) { }

        public PBKDF2(byte[] password, byte[] salt, int iterations)
        {
            Salt = salt;
            IterationCount = iterations;
            m_hmacsha1 = new HMACSHA1(password);
            Initialize();
        }

        public int IterationCount
        {
            get { return (int)m_iterations; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "ArgumentOutOfRange_NeedPosNum");
                Contract.EndContractBlock();
                m_iterations = (uint)value;
                Initialize();
            }
        }

        public byte[] Salt
        {
            get { return (byte[])m_salt.Clone(); }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                Contract.EndContractBlock();
                m_salt = (byte[])value.Clone();
                Initialize();
            }
        }

        [System.Security.SecuritySafeCritical]
        public override byte[] GetBytes(int cb)
        {
            if (cb <= 0)
                throw new ArgumentOutOfRangeException("cb", "ArgumentOutOfRange_NeedPosNum");
            Contract.EndContractBlock();
            byte[] password = new byte[cb];

            int offset = 0;
            int size = m_endIndex - m_startIndex;
            if (size > 0)
            {
                if (cb >= size)
                {
                    Buffer.BlockCopy(m_buffer, m_startIndex, password, 0, size);
                    m_startIndex = m_endIndex = 0;
                    offset += size;
                }
                else
                {
                    Buffer.BlockCopy(m_buffer, m_startIndex, password, 0, cb);
                    m_startIndex += cb;
                    return password;
                }
            }

            Contract.Assert(m_startIndex == 0 && m_endIndex == 0, "Invalid start or end index in the internal buffer.");

            while (offset < cb)
            {
                byte[] T_block = Func();
                int remainder = cb - offset;
                if (remainder > BlockSize)
                {
                    Buffer.BlockCopy(T_block, 0, password, offset, BlockSize);
                    offset += BlockSize;
                }
                else
                {
                    Buffer.BlockCopy(T_block, 0, password, offset, remainder);
                    offset += remainder;
                    Buffer.BlockCopy(T_block, remainder, m_buffer, m_startIndex, BlockSize - remainder);
                    m_endIndex += (BlockSize - remainder);
                    return password;
                }
            }
            return password;
        }

        public override void Reset()
        {
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (m_hmacsha1 != null)
                {
                    ((IDisposable)m_hmacsha1).Dispose();
                }

                if (m_buffer != null)
                {
                    Array.Clear(m_buffer, 0, m_buffer.Length);
                }
                if (m_salt != null)
                {
                    Array.Clear(m_salt, 0, m_salt.Length);
                }
            }
        }

        private void Initialize()
        {
            if (m_buffer != null)
                Array.Clear(m_buffer, 0, m_buffer.Length);
            m_buffer = new byte[BlockSize];
            m_block = 1;
            m_startIndex = m_endIndex = 0;
        }

        // This function is defined as follow :
        // Func (S, i) = HMAC(S || i) | HMAC2(S || i) | ... | HMAC(iterations) (S || i) 
        // where i is the block number. 
        private byte[] Func()
        {
            byte[] INT_block = Int(m_block);

            m_hmacsha1.TransformBlock(m_salt, 0, m_salt.Length, m_salt, 0);
            m_hmacsha1.TransformFinalBlock(INT_block, 0, INT_block.Length);
            byte[] temp = m_hmacsha1.Hash;
            m_hmacsha1.Initialize();

            byte[] ret = temp;
            for (int i = 2; i <= m_iterations; i++)
            {
                temp = m_hmacsha1.ComputeHash(temp);
                for (int j = 0; j < BlockSize; j++)
                {
                    ret[j] ^= temp[j];
                }
            }

            // increment the block count. 
            m_block++;
            return ret;
        }

        private static byte[] Int(uint i)
        {
            byte[] b = BitConverter.GetBytes(i);
            byte[] littleEndianBytes = { b[3], b[2], b[1], b[0] };
            return BitConverter.IsLittleEndian ? littleEndianBytes : b;
        }
    }
}
