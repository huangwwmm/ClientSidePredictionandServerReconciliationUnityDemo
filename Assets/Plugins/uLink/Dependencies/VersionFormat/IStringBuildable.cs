using System.Text;

namespace UnityPark.VersionFormat
{
	/// <summary>
	/// An interface for making types easier to build to string format.
	/// </summary>
	public interface IStringBuildable
	{
		/// <summary>
		/// Gets a StringBuilder from the caller and uses it to append string information about the instance.
		/// </summary>
		/// <param name="builder">A StringBuilder instance to append data to.</param>
		void BuildString(StringBuilder builder);
	}
}
