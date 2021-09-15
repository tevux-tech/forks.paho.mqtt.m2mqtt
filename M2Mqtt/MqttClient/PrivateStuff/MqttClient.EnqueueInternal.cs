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

using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        /// <summary>
        /// Enqueue a message into the internal queue
        /// </summary>
        private void EnqueueInternal(MqttMsgBase msg) {
            // enqueue is needed (or not)
            var enqueue = true;

            // if it is a PUBREL message (for QoS Level 2)
            if (msg.Type == MqttMsgBase.MessageType.PubRel) {
                lock (_inflightQueue) {
                    // if it is a PUBREL but the corresponding PUBLISH isn't in the inflight queue,
                    // it means that we processed PUBLISH message and received PUBREL and we sent PUBCOMP
                    // but publisher didn't receive PUBCOMP so it re-sent PUBREL. We need only to re-send PUBCOMP.

                    // NOTE : I need to find on message id and flow because the broker could be publish/received
                    //        to/from client and message id could be the same (one tracked by broker and the other by client)
                    var msgCtxFinder = new MqttMsgContextFinder(msg.MessageId, MqttMsgFlow.ToAcknowledge);
                    var msgCtx = (MqttMsgContext)_inflightQueue.Get(msgCtxFinder.Find);

                    // the PUBLISH message isn't in the inflight queue, it was already processed so
                    // we need to re-send PUBCOMP only
                    if (msgCtx == null) {
                        var pubcomp = new MqttMsgPubcomp {
                            MessageId = msg.MessageId
                        };

                        Send(pubcomp);

                        enqueue = false;
                    }
                }
            }
            // if it is a PUBCOMP message (for QoS Level 2)
            else if (msg.Type == MqttMsgBase.MessageType.PubComp) {
                lock (_inflightQueue) {
                    // if it is a PUBCOMP but the corresponding PUBLISH isn't in the inflight queue,
                    // it means that we sent PUBLISH message, sent PUBREL (after receiving PUBREC) and already received PUBCOMP
                    // but publisher didn't receive PUBREL so it re-sent PUBCOMP. We need only to ignore this PUBCOMP.

                    // NOTE : I need to find on message id and flow because the broker could be publish/received
                    //        to/from client and message id could be the same (one tracked by broker and the other by client)
                    var msgCtxFinder = new MqttMsgContextFinder(msg.MessageId, MqttMsgFlow.ToPublish);
                    var msgCtx = (MqttMsgContext)_inflightQueue.Get(msgCtxFinder.Find);

                    // the PUBLISH message isn't in the inflight queue, it was already sent so we need to ignore this PUBCOMP
                    if (msgCtx == null) {
                        enqueue = false;
                    }
                }
            }
            // if it is a PUBREC message (for QoS Level 2)
            else if (msg.Type == MqttMsgBase.MessageType.PubRec) {
                lock (_inflightQueue) {
                    // if it is a PUBREC but the corresponding PUBLISH isn't in the inflight queue,
                    // it means that we sent PUBLISH message more times (retries) but broker didn't send PUBREC in time
                    // the publish is failed and we need only to ignore this PUBREC.

                    // NOTE : I need to find on message id and flow because the broker could be publish/received
                    //        to/from client and message id could be the same (one tracked by broker and the other by client)
                    var msgCtxFinder = new MqttMsgContextFinder(msg.MessageId, MqttMsgFlow.ToPublish);
                    var msgCtx = (MqttMsgContext)_inflightQueue.Get(msgCtxFinder.Find);

                    // the PUBLISH message isn't in the inflight queue, it was already sent so we need to ignore this PUBREC
                    if (msgCtx == null) {
                        enqueue = false;
                    }
                }
            }

            if (enqueue) {
                lock (_internalQueue) {
                    _internalQueue.Enqueue(msg);
                    Trace.WriteLine(TraceLevel.Queuing, "enqueued {0}", msg);
                    _inflightWaitHandle.Set();
                }
            }
        }
    }


}
