using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerrariansConstructLib.API.Numbers {
	/// <summary>
	/// A helper class for converting positive, non-zero integers into their Roman number equivalents, using various formats<br/>
	/// Only numbers &gt; 0 and &lt;= 3999 are supported
	/// </summary>
	public static class Roman {
		//Used to optimize getting the Roman numeral for the same integer multiple times
		private static readonly Dictionary<int, string[]> cachedResults = new();

		private static readonly Dictionary<int, string[]> digits = new() {
			// 1
			[I] =
				new[] {      "I",  "I", "I" },
			// 4
			[V - I] =
				new[] {     "IV", "IV", "IIII" },
			// 5
			[V] =
				new[] {      "V",  "V", "V" },
			// 9
			[X - I] =
				new[] {     "IX", "IX", "VIIII" },
			// 10
			[X] =
				new[] {      "X",  "X", "X" },
			// 40
			[L - X] =
				new[] {     "XL", "XL", "XXXX" },
			// 49
			[L - I] =
				new[] {   "XLIX", "IL", "XXXXVIIII" },
			// 50
			[L] =
				new[] {      "L",  "L", "L" },
			// 90
			[C - X] =
				new[] {     "XC", "XC", "LXXXX" },
			// 99
			[C - I] =
				new[] {   "XCIX", "IC", "LXXXXVIIII" },
			// 100
			[C] =
				new[] {      "C",  "C", "C" },
			// 400
			[D - C] =
				new[] {     "CD", "CD", "CCCC" },
			// 490
			[D - X] =
				new[] {   "CDXC", "XD", "CCCCLXXXX" },
			// 499
			[D - I] =
				new[] { "CDXCIX", "ID", "CCCCLXXXXVIIII" },
			// 900
			[M - C] =
				new[] {     "CM", "CM", "DCCCC" },
			// 990
			[M - X] =
				new[] {   "CMXC", "XM", "DCCCCLXXXX" },
			// 999
			[M - I] =
				new[] { "CMXCIX", "IM", "DCCCCLXXXXVIIII" },
			// 1000
			[M] =
				new[] {      "M",  "M", "M" }
		};

		private static readonly int[] digitKeys = digits.Keys.ToArray();

		public const int I = 1, V = 5, X = 10, L = 50, C = 100, D = 500, M = 1000;

		public static string Convert(int number, RomanFormat format = RomanFormat.SubtractiveConventional) {
			if (number <= 0 || number > 3999)
				throw new ArgumentOutOfRangeException(nameof(number), "Number was " + number);

			if (format < RomanFormat.SubtractiveConventional || format > RomanFormat.Additive)
				throw new ArgumentOutOfRangeException(nameof(format));
			
			int fmt = (int)format;

			if (cachedResults.TryGetValue(number, out var arr) && arr[fmt] is not null)
				return arr[fmt];

			int orig = number;

			//Repeated string concatenation is heavy work for the GC due to strings being immutable
			StringBuilder sb = new();

			while (number > 0) {
				int digit = digitKeys.Length - 1;
				for (; digit > 0; digit--)
					if (number >= digitKeys[digit])
						break;

				sb.Append(digits[digitKeys[digit]][fmt]);

				number -= digitKeys[digit];
			}

			if (!cachedResults.ContainsKey(orig))
				cachedResults[orig] = new string[3];

			return cachedResults[orig][fmt] = sb.ToString();
		}
	}

	public enum RomanFormat {
		/// <summary>
		/// Subtractive format.  IX = 9, XLIX = 49, CMXCIX = 999
		/// </summary>
		SubtractiveConventional,
		/// <summary>
		/// Subtractive format.  IX = 9, IL = 49, IM = 999
		/// </summary>
		SubtractiveNonConventional,
		/// <summary>
		/// Additive format.  XVIIII = 9, XXXXVIIII = 49, DCCCCXXXXVIIII = 999
		/// </summary>
		Additive
	}
}
