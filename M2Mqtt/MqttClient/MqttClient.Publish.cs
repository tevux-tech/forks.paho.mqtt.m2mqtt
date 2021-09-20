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

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        /// <summary>
        /// Publish a message asynchronously (QoS Level 0 and not retained)
        /// </summary>
        /// <returns>Packet Id related to PUBLISH message</returns>
        public ushort Publish(string topic, byte[] message) {
            if (_isInitialized == false) { throw new InvalidOperationException("MqttClient has not been initialized. Call Initialize() method first."); }

            return Publish(topic, message, QosLevel.AtMostOnce, false);
        }

        /// <summary>
        /// Publish a message asynchronously
        /// </summary>
        /// <returns>Packet Id related to PUBLISH packet</returns>
        public ushort Publish(string topic, byte[] message, QosLevel qosLevel, bool retain) {
            if (_isInitialized == false) { throw new InvalidOperationException("MqttClient has not been initialized. Call Initialize() method first."); }

            var publishPacket = new PublishPacket(topic, message, qosLevel, retain);

            _outgoingPublishStateMachine.Publish(publishPacket);

            return publishPacket.PacketId;
        }
    }
}
