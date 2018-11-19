namespace UnityPark.VersionFormat
{
	///<summary>
	/// Signifies a type of build.
	///</summary>
	public enum VersionBuildType
	{
		/// <summary>
		/// Stable build, which has been released to the public.
		/// </summary>
		Stable,

		/// <summary>
		/// Beta build, which is a preview of the next stable.
		/// </summary>
		Beta,

		/// <summary>
		/// Custom build, which might have changes that will not enter into mainline releases.
		/// </summary>
		Custom,
	}
}
