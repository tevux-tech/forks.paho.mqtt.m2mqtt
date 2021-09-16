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
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        internal void Send(byte[] msgBytes) {
            try {
                // send message
                _channel.Send(msgBytes);

                // update last message sent ticks
                LastCommTime = Environment.TickCount;
            }
            catch (Exception e) {
                Trace.WriteLine(TraceLevel.Error, "Exception occurred: {0}", e.ToString());

                throw new MqttCommunicationException(e);
            }
        }

        internal void Send(ISentToBroker msg) {
            Trace.WriteLine(TraceLevel.Frame, "SEND {0}", msg);
            Send(msg.GetBytes());
        }
    }
}
