#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 11844 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2012-04-13 18:05:10 +0200 (Fri, 13 Apr 2012) $
#endregion

using System;
using System.Text;
using Lidgren.Network;

namespace uLink
{
	internal struct Password : IEquatable<Password>
	{
		public static Password empty = new Password(new byte[0]);

		public readonly byte[] hash;

		public Password(byte[] hash)
		{
			this.hash = hash;
		}

		public Password(string password)
		{
			hash = _ComputeHash(password);
		}

		public Password(NetBuffer buffer)
		{
			uint length = buffer.ReadVariableUInt32();
			hash = buffer.ReadBytes((int) length);
		}

		public void Write(NetBuffer buffer)
		{
			buffer.WriteVariableUInt32((uint) hash.Length);
			buffer.Write(hash);
		}

		public bool isEmpty { get { return hash.Length == 0; } }

		public override int GetHashCode() { return hash.Length; }

		public static bool operator ==(Password lhs, Password rhs) { return lhs.Equals(rhs); }
		public static bool operator !=(Password lhs, Password rhs) { return !lhs.Equals(rhs); }

		public override bool Equals(object other)
		{
			return (other is Password) && Equals((Password)other);
		}

		public bool Equals(Password other)
		{
			if (hash.Length != other.hash.Length)
			{
				return false;
			}

			for (int i = 0; i < hash.Length; i++)
			{
				if (hash[i] != other.hash[i]) return false;
			}

			return true;
		}

		public override string ToString()
		{
			return Convert.ToBase64String(hash);
		}

		private static byte[] _ComputeHash(string password)
		{
			if (String.IsNullOrEmpty(password)) return new byte[0]; // TODO: optimize by avoiding allocation

			// TODO: implement password hash and salt

			return Encoding.UTF8.GetBytes(password);
		}
	}
}
