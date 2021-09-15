using System;
using System.Collections;

namespace uPLibrary.Networking.M2Mqtt.Utility {

    public class ConcurrentQueue : Queue {

        public override int Count {
            get {
                lock (base.SyncRoot) {
                    return base.Count;
                }

            }
        }
        public override bool IsSynchronized { get { return true; } }

        public override void Clear() {
            lock (base.SyncRoot) {
                base.Clear();
            }
        }

        public override object Clone() {
            throw new NotImplementedException();
        }

        public override bool Contains(object obj) {
            throw new NotImplementedException();
        }
        public override void CopyTo(Array array, int index) {
            throw new NotImplementedException();
        }

        public override object Dequeue() {
            object returnObject;

            lock (base.SyncRoot) {
                returnObject = base.Dequeue();
            }

            return returnObject;
        }
        public bool TryDequeue(out object obj) {
            var success = false;

            lock (base.SyncRoot) {
                if (base.Count > 0) {
                    obj = base.Dequeue();
                    success = true;
                }
                else {
                    obj = new object();
                }
            }

            return success;
        }

        public override void Enqueue(object obj) {
            lock (base.SyncRoot) {
                base.Enqueue(obj);
            }
        }

        public override IEnumerator GetEnumerator() {
            throw new NotImplementedException();
        }

        public override object Peek() {
            object returnObject;

            lock (base.SyncRoot) {
                returnObject = base.Peek();
            }

            return returnObject;
        }

        public override object[] ToArray() {

            object[] returnObjects;

            lock (base.SyncRoot) {
                returnObjects = base.ToArray();
            }

            return returnObjects;

        }

        public override void TrimToSize() {
            throw new NotImplementedException();
        }
    }
}
