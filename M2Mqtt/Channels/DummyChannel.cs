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
*/

using System.Net;

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// Dummy channel. Needed for the asynchronous state machines to work while actual connection has not been made.
    /// </summary>
    internal class DummyChannel : IMqttNetworkChannel {
        public string RemoteHostName { get; private set; }
        public IPAddress RemoteIpAddress { get; private set; }
        public int RemotePort { get; private set; }

        public bool DataAvailable { get { return false; } }

        public bool IsConnected { get; private set; } = false;

        public bool TryConnect() {
            return false;
        }

        public bool TrySend(byte[] buffer) {
            return false;
        }

        public bool TryReceive(byte[] buffer) {
            return false;
        }

        public void Close() { }
    }
}
