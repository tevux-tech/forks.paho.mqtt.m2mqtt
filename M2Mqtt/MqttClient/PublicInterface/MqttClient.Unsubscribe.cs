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
        public bool UnsubscribeAndWait(string topic) {
            return InternalUnsubscribe(topic, true);
        }
        public void Unsubscribe(string topic) {
            _ = InternalUnsubscribe(topic, false);
        }

        /// <summary>
        /// Unsubscribe for message topics
        /// </summary>
        /// <param name="topics">List of topics to unsubscribe</param>
        /// <returns>Packet Id in UNSUBACK packet from broker</returns>
        public bool InternalUnsubscribe(string topic, bool waitForCompletion) {
            if (_isInitialized == false) { throw new InvalidOperationException("MqttClient has not been initialized. Call Initialize() method first."); }

            var unsubscribePacket = new UnsubscribePacket(topic);
            var isUnsubscribed = _subscriptionStateMachine.Unsubscribe(unsubscribePacket, waitForCompletion);

            return isUnsubscribed;
        }
    }
}
