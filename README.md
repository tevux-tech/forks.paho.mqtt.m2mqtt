[![NuGet](https://img.shields.io/nuget/dt/Tevux.M2Mqtt.svg?label=NuGet&style=flat&logo=nuget)](https://www.nuget.org/packages/Tevux.M2Mqtt/) 
[![NuGet](https://img.shields.io/nuget/vpre/Tevux.M2Mqtt.svg?label=NuGet&style=flat&logo=nuget)](https://www.nuget.org/packages/Tevux.M2Mqtt/) 

# M2Mqtt

![](images/m2mqtt6.png)

M2Mqtt is a MQTT-3.1.1 client available for all .NET Core platforms for Internet of Things and M2M communication.

MQTT, short for Message Queue Telemetry Transport, is a light weight messaging protocol that enables embedded devices with limited resources to perform asynchronous communication on a constrained network.

MQTT protocol is based on publish/subscribe pattern so that a client can subscribe to one or more topics and receive messages that other clients publish on these topics.

For all information about MQTT protocol, please visit official web site  http://mqtt.org/.

# Sample

There a [test app](TestApp/Program.cs) to get started quickly.

# Architectural overview

Quick notes:
- MQTT specification allows to subscribe/unsubscribe multiple topics in one packet, but this implementation provides methods to only do that one by one. This is by design (to simplify usage and implementation).
- Exceptions are suppressed. They are only allowed in initialization phase, so that user program fails early if something is misconfigured.

## Transport channels

MQTT traffic is actually decoupled from the transport method, so that multiple channels could be utilized by implementing ```IMqttNetworkChannel``` interface. Currently, secure and unsecure TCP channels are implemented. MQTT specification requires data channels that guarantee ordered packet transmission and specifically rules UDP out, but this can still be done if limiting packet size so it does not get split along the route.

## Tickable state machines

MQTT packets could be sorted into specific categories:
- Ping (PINGREQ, PINGRESP)
- Connection (CONNECT, CONACK, DISCONNECT)
- Subscription (SUBSCRIBE, SUBACK)
- Unsubscription (UNSUBSCRIBE, UNSUBACK)
- Publishing (PUBLISH, PUBACK, PUBREC, PUBREL, PUBCOMP)

Each of these categories are completely isolated. They do not interract with each other, and thus can be implemented as completely independent state machines, that are periodically ticked from the ```MasterTickThread```. This way, if there's a bug in subscription process, it will not propagate into publishing, and vice versa.

## Event queue

```MqttClient``` class exposes user-consumable events like ```Connected```, ```PublishReceived``` and others. They are raised from the corresponding state machines, so it is critical user does not block them with long-running code. Otherwise, state machine will block, too, which cannot be allowed. For this reason, there is a separate event queue. ```ProcessEventQueueThread``` periodically checks the queue, and if there are events to raise, those are raised in a separate thread.