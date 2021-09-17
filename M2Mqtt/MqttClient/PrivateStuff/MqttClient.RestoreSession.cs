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

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        private void RestoreSession() {
            // if not clean session
            //if (!CleanSession) {
            //    // there is a previous session
            //    if (_session != null) {
            //        lock (_inflightQueue) {
            //            foreach (MqttMsgContext msgContext in _session.InflightMessages.Values) {
            //                _inflightQueue.Enqueue(msgContext);

            //                // if it is a PUBLISH message to publish
            //                if ((msgContext.Message.Type == MqttMsgBase.MessageType.Publish) &&
            //                    (msgContext.Flow == MqttMsgFlow.ToPublish)) {
            //                    // it's QoS 1 and we haven't received PUBACK
            //                    if ((msgContext.Message.QosLevel == QosLevel.AtLeastOnce) && (msgContext.State == MqttMsgState.WaitForPuback)) {
            //                        // we haven't received PUBACK, we need to resend PUBLISH message
            //                        msgContext.State = MqttMsgState.QueuedQos1;
            //                    }
            //                    // it's QoS 2
            //                    else if (msgContext.Message.QosLevel == QosLevel.ExactlyOnce) {
            //                        // we haven't received PUBREC, we need to resend PUBLISH message
            //                        if (msgContext.State == MqttMsgState.WaitForPubrec) {
            //                            msgContext.State = MqttMsgState.QueuedQos2;
            //                        }
            //                        // we haven't received PUBCOMP, we need to resend PUBREL for it
            //                        else if (msgContext.State == MqttMsgState.WaitForPubcomp) {
            //                            msgContext.State = MqttMsgState.SendPubrel;
            //                        }
            //                    }
            //                }
            //            }
            //        }

            //        // unlock process inflight queue
            //        _inflightWaitHandle.Set();
            //    }
            //    else {
            //        // create new session
            //        _session = new MqttSession(ClientId);
            //    }
            //}
            //// clean any previous session
            //else {
            //    if (_session != null) {
            //        _session.Clear();
            //    }
            //}
        }
    }
}
