﻿/*
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
        /// <summary>
        /// Publish a message asynchronously (QoS Level 0 and not retained)
        /// </summary>
        /// <returns>Message Id related to PUBLISH message</returns>
        public ushort Publish(string topic, byte[] message) {
            return Publish(topic, message, QosLevel.AtMostOnce, false);
        }

        /// <summary>
        /// Publish a message asynchronously
        /// </summary>
        /// <returns>Message Id related to PUBLISH message</returns>
        public ushort Publish(string topic, byte[] message, QosLevel qosLevel, bool retain) {
            // topic can't contain wildcards
            if ((topic.IndexOf('#') != -1) || (topic.IndexOf('+') != -1)) { throw new ArgumentException("Topic cannot contain wildcards in Publish message"); }

#warning check for topic length maybe?

            var publish = new MqttMsgPublish(topic, message, false, qosLevel, retain) {
                MessageId = GetNewMessageId()
            };

            _outgoingPublishStateMachine.Publish(publish);

            return publish.MessageId;
        }
    }
}
