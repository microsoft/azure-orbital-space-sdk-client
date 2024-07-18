import logging
import os
import sys
import time

root_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.realpath(__file__))))
sys.path.append(root_dir)

import spacefx

from spacefx.protos.sensor.Sensor_pb2 import SensorData
from spacefx.protos.common.Common_pb2 import StatusCodes

logger = spacefx.logger(level=logging.INFO)


def process_sensor_data(sensor_data: SensorData):
    logger.info(f"Received sensor data:")
    logger.info(f"TrackingId: {sensor_data.responseHeader.trackingId}")
    logger.info(f"SensorID: {sensor_data.sensorID}")
    logger.info(f"Data: {sensor_data.data}")


def sensor_service():
    logger.info("----SENSOR SERVICE: START-----")
    spacefx.sensor.subscribe_to_sensor_data(callback_function=process_sensor_data)

    logger.info("Querying available sensors")
    sensor_response = spacefx.sensor.get_available_sensors()
    logger.info("Sensor Response Heard")
    logger.info(f"    AppID: {sensor_response.responseHeader.appId}")
    logger.info(f"    TrackingId: {sensor_response.responseHeader.trackingId}")
    logger.info(f"    Status: {StatusCodes.Name(sensor_response.responseHeader.status)}")
    logger.info(f"    Message: {sensor_response.responseHeader.message}")
    logger.info(f"    Sensors: {sensor_response.sensors}")

    logger.info("Triggering a Tasking PreCheck for DemoTemperatureSensor")
    tasking_precheck_response = spacefx.sensor.sensor_tasking_pre_check("DemoTemperatureSensor")
    logger.info(f"Response: {StatusCodes.Name(tasking_precheck_response.responseHeader.status)}")

    logger.info("Triggering a Tasking for DemoTemperatureSensor")
    tasking_response = spacefx.sensor.sensor_tasking("DemoTemperatureSensor")
    logger.info(f"Response: {StatusCodes.Name(tasking_response.responseHeader.status)}")

    request_data = SensorData()
    payload_metadata = {"SOURCE_PAYLOAD_APP_ID": "earthobservationpythonapp"}

    logger.info("Triggering a FULL Tasking PreCheck for DemoTemperatureSensor")
    tasking_precheck_response = spacefx.sensor.sensor_tasking_pre_check("DemoTemperatureSensor", request_data, payload_metadata)
    logger.info(f"Response: {StatusCodes.Name(tasking_precheck_response.responseHeader.status)}")

    logger.info("Triggering a FULL Tasking for DemoTemperatureSensor")
    tasking_response = spacefx.sensor.sensor_tasking("DemoTemperatureSensor",  request_data, payload_metadata)
    logger.info(f"Response: {StatusCodes.Name(tasking_response.responseHeader.status)}")
    logger.info("----SENSOR SERVICE: END-----")

    logger.info("----SENSOR SERVICE: END-----")


def link_service():
    logger.info("----LINK SERVICE: START-----")

    logger.info("Querying for xfer Directories...")
    xfer_directory = spacefx.link.get_xfer_directories()
    logger.info("Outbox: %s" % xfer_directory['outbox'])
    logger.info("Inbox: %s" % xfer_directory['inbox'])
    logger.info("Root: %s" % xfer_directory['root'])

    logger.info("Sending file to app...")
    testfile = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "sampleData", "astronaut.jpg")
    link_response = spacefx.link.send_file_to_app("spacesdk-client", testfile, overwrite_destination_file=True)
    logger.info(f"Result: {StatusCodes.Name(link_response.responseHeader.status)}")
    logger.info("----LINK SERVICE: END-----")


def logging_service():
    logger.info("----LOGGING SERVICE: START-----")
    logger.info("Sending a telemetry message to the logging service...")
    telemetry_response = spacefx.logging.send_telemetry("test_metric", 12345)
    logger.info(f"Telemetry Response: {StatusCodes.Name(telemetry_response.responseHeader.status)}")

    logger.info("Triggering 1000 logs to the logging service at debug level...")
    for i in range(1000):
        logger.debug("Trigger log #%s" % i)

    logger.info("Successfully triggered 1000 logs to the logging service")
    logger.info("----LOGGING SERVICE: END-----")


def position_service():
    logger.info("----POSITION SERVICE: START-----")
    logger.info("Querying for current position")
    current_pos = spacefx.position.request_position()
    logger.info(f"Status: {StatusCodes.Name(current_pos.responseHeader.status)}")
    logger.info(f"Current position: {current_pos.position.point}")
    logger.info("----POSITION SERVICE: END-----")


def main():
    print("Building SpaceFX Client")
    spacefx.client.build()

    appid = spacefx.client.get_app_id()
    logger.info(f"AppID: {appid}")

    config_dir = spacefx.client.get_config_dir()
    logger.info(f"Config Dir: {config_dir}")

    logger.info("Sleeping for 5 seconds to allow for heartbeats to trickle in...")
    time.sleep(5)

    logger.info("Listing services online (heartbeats heard)")
    services_online = spacefx.client.services_online()
    for _, appId in enumerate(services_online):
        logger.info(f"    AppID: {appId.AppId}")

    position_service()
    sensor_service()
    link_service()
    logging_service()


if __name__ == '__main__':
    main()
