import socket
import struct
import json
import paho.mqtt.client as mqtt

### Config

MQTT_BROKER = '192.168.1.5'
MQTT_USER_NAME = 'device'
MQTT_PASSWORD = 'zzdev1ce'

ADDR_TO_NAME = {
    '192.168.1.69': 'AM7P'
}

### Config end

CONFIG_TOPIC_FORMAT = "homeassistant/sensor/{}/config"
STATE_TOPIC_FORMAT = "homeassistant/sensor/{}/state"

SENSORS = [
    {
        "name": "PM2.5",
        "device_class": "pm25",
        "unit_of_measurement": "µg/m³",
        "value_template": "{{ value_json.pm25 }}"
    },
    {
        "name": "PM10",
        "device_class": "pm10",
        "unit_of_measurement": "µg/m³",
        "value_template": "{{ value_json.pm10 }}"
    },
    {
        "name": "HCHO",
        "unique_id": "hcho",
        "device_class": "volatile_organic_compounds",
        "unit_of_measurement": "mg/m³",
        "value_template": "{{ value_json.hcho }}"
    },
    {
        "name": "TVOC",
        "unique_id": "tvoc",
        "device_class": "volatile_organic_compounds",
        "unit_of_measurement": "mg/m³",
        "value_template": "{{ value_json.tvoc }}"
    },
    {
        "name": "CO2",
        "unique_id": "co2",
        "device_class": "carbon_dioxide",
        "unit_of_measurement": "ppm",
        "value_template": "{{ value_json.co2 }}"
    },
    {
        "name": "Temperature",
        "device_class": "temperature",
        "unit_of_measurement": "°C",
        "value_template": "{{ value_json.temperature }}"
    },
    {
        "name": "Humidity",
        "device_class": "humidity",
        "unit_of_measurement": "%",
        "value_template": "{{ value_json.humidity }}"
    }
]

def get_name_by_addr(addr):
    return ADDR_TO_NAME[addr] if addr in ADDR_TO_NAME else 'am7p'

def auto_discovery(client: mqtt.Client, addr):
    name = get_name_by_addr(addr)
    state_topic = STATE_TOPIC_FORMAT.format(name)

    for sensor in SENSORS:
        config = dict(sensor)
        config.update({
            "state_topic": state_topic,
            "unique_id": "{}_{}".format(name, sensor['unique_id'] if 'unique_id' in sensor else sensor['device_class']),
            "device": {
                "identifiers": [ "airmaster_{}".format(name) ],
                "name": name
            }
        })

        client.publish(CONFIG_TOPIC_FORMAT.format(config['unique_id']), json.dumps(config))


def listen_and_send_to_mqtt(client: mqtt.Client):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind(("", 12414))

    known_addrs = set()

    while True:
        data, addr = sock.recvfrom(1024)
        
        ip = addr[0]
        if ip not in known_addrs:
            print(ip)
            known_addrs.add(ip)
            auto_discovery(client, ip)
        
        x = struct.unpack_from('>HHHHHHH', data[23:])
        state = {
            'pm25': x[0],
            'pm10': x[1],
            'hcho': x[2] / 100,
            'tvoc': x[3] / 100,
            'co2': x[4],
            'temperature': (x[5] - 3500) / 100,
            'humidity': x[6] / 100
        }

        name = get_name_by_addr(ip)
        client.publish(STATE_TOPIC_FORMAT.format(name), json.dumps(state))


def main():
    client = mqtt.Client()
    client.connect(MQTT_BROKER)
    client.username_pw_set(MQTT_USER_NAME, MQTT_PASSWORD)
    client.loop_start()

    listen_and_send_to_mqtt(client)


if __name__ == '__main__':
    main()