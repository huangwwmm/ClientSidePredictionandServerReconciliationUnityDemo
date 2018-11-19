#region COPYRIGHT
// (c)2011 Unity Park. All Rights Reserved.
// 
// $Revision: 8418 $
// $LastChangedBy: aidin $
// $LastChangedDate: 2011-08-11 16:26:10 +0200 (Thu, 11 Aug 2011) $
#endregion
#define UNITY_BUILD //by WuNan @2016/09/18 添加uLink源码工程中的条件编译符号
using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RPC : Attribute
{
	public RPC() { }
}

namespace uLink
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class RPC : Attribute
	{
		public RPC() { }
	}

#if !UNITY_BUILD
#if !LOAD_TESTER_BUILD
	public struct Vector2
	{
		public static readonly Vector2 zero = new Vector2(0, 0);

		public float x;
		public float y;

		public Vector2(float x, float y)
		{
			this.x = x; this.y = y;
		}

		public static bool operator ==(Vector2 a, Vector2 b)
		{
			return a.x == b.x & a.y == b.y;
		}

		public static bool operator !=(Vector2 a, Vector2 b)
		{
			return a.x != b.x | a.y != b.y;
		}
	}

	public struct Vector3
	{
		public static readonly Vector3 zero = new Vector3(0, 0, 0);

		public float x;
		public float y;
		public float z;

		public Vector3(float x, float y, float z)
		{
			this.x = x; this.y = y; this.z = z;
		}
	
		public override string ToString()
		{
			return "(" + x + ", " + y + ", " + z + ")";
		}

		public static Vector3 operator +(Vector3 a, Vector3 b)
		{
			return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
		}

		public static bool operator ==(Vector3 a, Vector3 b)
		{
			return a.x == b.x & a.y == b.y & a.z == b.z;
		}

		public static bool operator !=(Vector3 a, Vector3 b)
		{
			return a.x != b.x | a.y != b.y | a.z != b.z;
		}
	}

	public struct Vector4
	{
		public static readonly Vector4 zero = new Vector4(0, 0, 0, 0);

		public float x;
		public float y;
		public float z;
		public float w;

		public Vector4(float x, float y, float z, float w)
		{
			this.x = x; this.y = y; this.z = z; this.w = w;
		}
	
		public override string ToString()
		{
			return "(" + x + ", " + y + ", " + z + ", " + w + ")";
		}

		public static bool operator ==(Vector4 a, Vector4 b)
		{
			return a.x == b.x & a.y == b.y & a.z == b.z & a.w == b.w;
		}

		public static bool operator !=(Vector4 a, Vector4 b)
		{
			return a.x != b.x | a.y != b.y | a.z != b.z | a.w != b.w;
		}
	}

	public struct Quaternion
	{
		public static readonly Quaternion identity = new Quaternion(0, 0, 0, 1);

		public float x;
		public float y;
		public float z;
		public float w;

		public Quaternion(float x, float y, float z, float w)
		{
			this.x = x; this.y = y; this.z = z; this.w = w;
		}
	
		public override string ToString()
		{
			return "(" + x + ", " + y + ", " + z + ", " + w + ")";
		}

		public static Quaternion operator *(Quaternion lhs, Quaternion rhs)
		{
			return new Quaternion(lhs.w * rhs.x + lhs.x * rhs.w + lhs.y * rhs.z - lhs.z * rhs.y, lhs.w * rhs.y + lhs.y * rhs.w + lhs.z * rhs.x - lhs.x * rhs.z, lhs.w * rhs.z + lhs.z * rhs.w + lhs.x * rhs.y - lhs.y * rhs.x, lhs.w * rhs.w - lhs.x * rhs.x - lhs.y * rhs.y - lhs.z * rhs.z);
		}

		public static bool operator ==(Quaternion a, Quaternion b)
		{
			return a.x == b.x & a.y == b.y & a.z == b.z & a.w == b.w;
		}

		public static bool operator !=(Quaternion a, Quaternion b)
		{
			return a.x != b.x | a.y != b.y | a.z != b.z | a.w != b.w;
		}
	}
#endif

	public struct Color
	{
		public float r;
		public float g;
		public float b;
		public float a;

		public Color(float r, float g, float b, float a)
		{
			this.r = r; this.g = g; this.b = b; this.a = a;
		}
	
		public override string ToString()
		{
			return "(" + r + ", " + g + ", " + b + ")";
		}

		public static bool operator ==(Color a, Color b)
		{
			return a.r == b.r & a.g == b.g & a.b == b.b & a.a == b.a;
		}

		public static bool operator !=(Color a, Color b)
		{
			return a.r != b.r | a.g != b.g | a.b != b.b | a.a != b.a;
		}
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class SerializeField : Attribute
	{
		public SerializeField() { }
	}

	public static class Profiler
	{
		public static void BeginSample(string name) {}
		public static void EndSample() {}
	}

#endif
}
