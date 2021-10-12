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

using System.Threading;

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        public void Initialize() {
            _pingStateMachine.Initialize(this);
            _connectStateMachine.Initialize(this);
            _subscriptionStateMachine.Initialize(this);
            _outgoingPublishStateMachine.Initialize(this);
            _incomingPublishStateMachine.Initialize(this);

            new Thread(MasterTickThread).Start();
            new Thread(ReceiveThread).Start();
            new Thread(EventQueueThread).Start();

            _isInitialized = true;
        }
    }
}
