using System.Collections.Generic;
using System.Runtime.Loader;
using TerrariansConstructLib.Materials;

namespace TerrariansConstructLib.API {
	public class PartsDictionary<T> : Dictionary<int, Dictionary<int, T>> {
		public T Get(Material material, int partID)
			=> this.GetValueFromPartDictionary(material, partID);

		public bool TryGet(Material material, int partID, out T value)
			=> this.TryGetValueFromPartDictionary(material, partID, out value);

		public void Set(Material material, int partID, T value)
			=> this.SafeSetValueInPartDictionary(material, partID, value);
	}
}
