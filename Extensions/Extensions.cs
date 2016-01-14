using System;

namespace Eve.Extensions
{
	public static class Extentions {
		/// <summary>
		///     Compares the object to a string with default ignorance of casing
		/// </summary>
		/// <param name="query">string to compare</param>
		/// <param name="ignoreCase">whether or not to ignore case</param>
		/// <returns>true: strings equal; false: strings unequal</returns>
		public static bool CaseEquals(this string obj, string query, bool ignoreCase = true) {
			return obj.Equals(query, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
		}
	}
}
