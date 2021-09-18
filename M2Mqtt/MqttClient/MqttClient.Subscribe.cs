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

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        public ushort Subscribe(string topic, QosLevel qosLevel) {
            if (string.IsNullOrEmpty(topic)) { throw new ArgumentException($"Argument '{nameof(topic)}' has to be a valid non-empty string", nameof(topic)); }

            var subscribeMessage = new MqttMsgSubscribe(topic, qosLevel) {
                MessageId = GetNewMessageId()
            };

            _subscribeStateMachine.Subscribe(subscribeMessage);

            return subscribeMessage.MessageId;
        }
    }
}
