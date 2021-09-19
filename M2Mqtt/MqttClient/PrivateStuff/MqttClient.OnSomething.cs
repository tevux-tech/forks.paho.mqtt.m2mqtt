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
        /// <summary>
        /// Wrapper method for raising PUBLISH message received event
        /// </summary>
        /// <param name="publish">PUBLISH message received</param>
        internal void OnMqttMsgPublishReceived(MqttMsgPublish publish) {
            PublishReceived?.Invoke(this, new PublishReceivedEventArgs(publish.Topic, publish.Message));
        }

        /// <summary>
        /// Wrapper method for raising published message event
        /// </summary>
        /// <param name="messageId">Message identifier for published message</param>
        /// <param name="isPublished">Publish flag</param>
        internal void OnMqttMsgPublished(ushort messageId, bool isPublished) {
            Published?.Invoke(this, new PublishFinishedEventArgs(messageId, isPublished));
        }

        /// <summary>
        /// Wrapper method for raising subscribed topic event
        /// </summary>
        /// <param name="suback">SUBACK message received</param>
        internal void OnMqttMsgSubscribed(MqttMsgSuback suback) {
            Subscribed?.Invoke(this, new SubscribedEventArgs(suback.MessageId, suback.GrantedQosLevels));
        }

        /// <summary>
        /// Wrapper method for raising unsubscribed topic event
        /// </summary>
        /// <param name="messageId">Message identifier for unsubscribed topic</param>
        internal void OnMqttMsgUnsubscribed(ushort messageId) {
            Unsubscribed?.Invoke(this, new UnsubscribedEventArgs(messageId));
        }

        /// <summary>
        /// Wrapper method for peer/client disconnection
        /// </summary>
        private void OnConnectionClosed() {
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
