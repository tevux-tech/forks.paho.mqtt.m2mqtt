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

using System;
using System.Threading;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        /// <summary>
        /// Thread for handling keep alive message
        /// </summary>
        private void KeepAliveThread() {
            var wait = _keepAlivePeriod;

            // create event to signal that current thread is end
            _keepAliveEventEnd = new AutoResetEvent(false);

            while (_isRunning) {
                // waiting...
                _keepAliveEvent.WaitOne(wait);

                if (_isRunning) {
                    var delta = Environment.TickCount - _lastCommTime;

                    // if timeout exceeded ...
                    if (delta >= _keepAlivePeriod) {
                        // ... send keep alive
                        Ping();
                        wait = _keepAlivePeriod;
                    }
                    else {
                        // update waiting time
                        wait = _keepAlivePeriod - delta;
                    }
                }
            }

            // signal thread end
            _keepAliveEventEnd.Set();
        }
    }
}
