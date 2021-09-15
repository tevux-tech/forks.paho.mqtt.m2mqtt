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
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {

    public partial class MqttClient {
        /// <summary>
        /// Execute ping to broker for keep alive
        /// </summary>
        /// <returns>PINGRESP message from broker</returns>
        private MqttMsgPingResp Ping() {
            var pingreq = new MqttMsgPingReq();
            try {
                // broker must send PINGRESP within timeout equal to keep alive period
                return (MqttMsgPingResp)SendReceive(pingreq, _keepAlivePeriod);
            }
            catch (Exception e) {
                Trace.WriteLine(TraceLevel.Error, "Exception occurred: {0}", e.ToString());

                // client must close connection
                OnConnectionClosing();
                return null;
            }
        }
    }
}
