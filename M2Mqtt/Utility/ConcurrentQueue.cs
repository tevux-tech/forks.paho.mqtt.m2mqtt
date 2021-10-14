/*
Copyright (c) 2013, 2014 Paolo Patierno

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution. 

The Eclipse Public License is available at 
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at 
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Paolo Patierno - initial API and implementation and/or initial documentation
   Simonas Greicius - 2021 rework
*/

using System;
using System.Collections;

namespace Tevux.Protocols.Mqtt.Utility {
    /// <summary>
    /// A local thread-safe queue which does not use generics.
    /// </summary>
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
