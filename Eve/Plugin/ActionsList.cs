#region usings

using System;
using System.Collections;
using System.Linq;

#endregion

namespace Eve.Plugin {
    public class ActionList<TAction> where TAction : MarshalByRefObject, ICollection, IList {
        private readonly TAction[] internalList = {};

        public TAction this[int index] {
            get { return internalList[index]; }
            set { internalList[index] = value; }
        }

        public bool IsReadOnly { get; } = false;

        public int Count => internalList.Length;

        public object SyncRoot { get; }
        public bool IsSynchronized { get; }

        public void CopyTo(Array array, int index) {
            internalList.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator() {
            throw new NotImplementedException();
        }

        public TAction IndexOf(int index) {
            return internalList[index];
        }

        public void Add(TAction item) {
            internalList[internalList.Length + 1] = item;
        }

        public void Clear() {
            Array.Clear(internalList, 0, internalList.Length);
        }

        public bool Contains(TAction item) {
            return internalList.Any(action => action == item);
        }

        public void CopyTo(TAction[] array, int arrayIndex) {
            internalList.CopyTo(array, arrayIndex);
        }

        public bool Remove(TAction item) {
            int priorLength = internalList.Length;

            for (int i = 0; i < internalList.Length; i++)
                if (internalList[i] == item)
                    RemoveAt(i);

            return priorLength.Equals(internalList.Length);
        }

        public TAction[] RemoveAt(int index) {
            var destination = new TAction[internalList.Length - 1];

            if (index > 0)
                Array.Copy(internalList, 0, destination, 0, index);

            if (index < internalList.Length - 1)
                Array.Copy(internalList, index + 1, destination, index, internalList.Length - 1);

            return destination;
            ;
        }
    }
}