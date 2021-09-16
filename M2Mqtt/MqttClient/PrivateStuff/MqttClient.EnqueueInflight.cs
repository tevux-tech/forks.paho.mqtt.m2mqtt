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

using System.Linq;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Internal;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        /// <summary>
        /// Enqueue a message into the inflight queue
        /// </summary>
        /// <param name="flow">Message flow (publish, acknowledge)</param>
        /// <returns>Message enqueued or not</returns>
        private bool EnqueueInflight(MqttMsgBase msg, MqttMsgFlow flow) {
            // enqueue is needed (or not)
            var enqueue = true;

            // if it is a PUBLISH message with QoS Level 2
            if ((msg.Type == MqttMsgBase.MessageType.Publish) && (msg.QosLevel == QosLevel.ExactlyOnce)) {
                lock (_inflightQueue) {
                    // if it is a PUBLISH message already received (it is in the inflight queue), the publisher
                    // re-sent it because it didn't received the PUBREC. In this case, we have to re-send PUBREC

                    // NOTE : I need to find on message id and flow because the broker could be publish/received
                    //        to/from client and message id could be the same (one tracked by broker and the other by client)
                    var msgCtxFinder = new MqttMsgContextFinder(msg.MessageId, MqttMsgFlow.ToAcknowledge);
#if (NETSTANDARD1_6 || NETCOREAPP3_1)
                    var msgCtx = (MqttMsgContext)_inflightQueue.ToArray().FirstOrDefault(msgCtxFinder.Find);
#else
                    MqttMsgContext msgCtx = (MqttMsgContext)this.inflightQueue.Get(msgCtxFinder.Find);
#endif

                    // the PUBLISH message is alredy in the inflight queue, we don't need to re-enqueue but we need
                    // to change state to re-send PUBREC
                    if (msgCtx != null) {
                        msgCtx.State = MqttMsgState.QueuedQos2;
                        msgCtx.Flow = MqttMsgFlow.ToAcknowledge;
                        enqueue = false;
                    }
                }
            }

            if (enqueue) {
                // set a default state
                var state = MqttMsgState.QueuedQos0;

                // based on QoS level, the messages flow between broker and client changes
                switch (msg.QosLevel) {
                    case QosLevel.AtMostOnce:
                        state = MqttMsgState.QueuedQos0;
                        break;

                    case QosLevel.AtLeastOnce:
                        state = MqttMsgState.QueuedQos1;
                        break;

                    case QosLevel.ExactlyOnce:
                        state = MqttMsgState.QueuedQos2;
                        break;
                }

                // [v3.1.1] SUBSCRIBE and UNSUBSCRIBE aren't "officially" QOS = 1
                //          so QueuedQos1 state isn't valid for them
                //if (msg.Type == MqttMsgBase.MessageType.Subscribe) {
                //    state = MqttMsgState.SendSubscribe;
                //}
                //else if (msg.Type == MqttMsgBase.MessageType.Unsubscribe) {
                //    state = MqttMsgState.SendUnsubscribe;
                //}

                // queue message context
                var msgContext = new MqttMsgContext() {
                    Message = msg,
                    State = state,
                    Flow = flow,
                    Attempt = 0
                };

                lock (_inflightQueue) {
                    // check number of messages inside inflight queue 
                    enqueue = (_inflightQueue.Count < _settings.InflightQueueSize);

                    if (enqueue) {
                        // enqueue message and unlock send thread
                        _inflightQueue.Enqueue(msgContext);

                        Trace.WriteLine(TraceLevel.Queuing, "enqueued {0}", msg);

                        // PUBLISH message
                        if (msg.Type == MqttMsgBase.MessageType.Publish) {
                            // to publish and QoS level 1 or 2
                            if ((msgContext.Flow == MqttMsgFlow.ToPublish) &&
                                ((msg.QosLevel == QosLevel.AtLeastOnce) || (msg.QosLevel == QosLevel.ExactlyOnce))) {
                                if (_session != null) {
                                    _session.InflightMessages.Add(msgContext.Key, msgContext);
                                }
                            }
                            // to acknowledge and QoS level 2
                            else if ((msgContext.Flow == MqttMsgFlow.ToAcknowledge) && (msg.QosLevel == QosLevel.ExactlyOnce)) {
                                if (_session != null) {
                                    _session.InflightMessages.Add(msgContext.Key, msgContext);
                                }
                            }
                        }
                    }
                }
            }

            _inflightWaitHandle.Set();

            return enqueue;
        }
    }


}
