from typing import Dict, TypeVar, Callable
from threading import Thread

from google.protobuf.any_pb2 import Any

from spacefx.protos.sensor.Sensor_pb2 import \
    SensorsAvailableResponse, \
    SensorData, \
    TaskingPreCheckResponse, \
    TaskingResponse

from System.Collections.Generic import Dictionary
from System import String, Array, Byte

import Google.Protobuf.WellKnownTypes

from spacefx._sdk_client import __sdk_sensor, __sdk_core, __sdk_client, __sdk_utils

T = TypeVar("T")
_sensor_data_subscribers = []


def get_xfer_directories() -> dict[str]:
    """
    Returns the inbox, outbox, and root of the xfer volume within hostsvc-link
    """
    _task = __sdk_core.GetXFerDirectories()
    _task.Wait()

    return {
        "inbox": _task.Result.Item1,
        "outbox": _task.Result.Item2,
        "root": _task.Result.Item3
    }


def get_available_sensors(response_timeout_seconds=30) -> SensorsAvailableResponse:
    """
    Queries the Sensor Host Service for sensors that are available to the application

    Args:
        response_timeout_seconds (int, optional): the number of seconds to wait for a successful SensorsAvailableResponse
    Returns:
        response (SensorsAvailableResponse): A SUCCESSFUL SensorsAvailableResponse, or the last heard SensorsAvailableResponse during the timeout period
    Raises:
        TimeoutError: Raises a TimeoutError if no SensorsAvailableResponse message was heard during the timeout period
    """
    _task = __sdk_sensor.GetAvailableSensors(responseTimeoutSecs=response_timeout_seconds)
    _task.Wait()

    # This converts the response from a dotnet object to a python object to insure transparent implementation
    response = SensorsAvailableResponse()
    result_bytes = bytes(__sdk_utils.ConvertProtoToBytes(_task.Result))
    response.ParseFromString(result_bytes)

    return response


def sensor_tasking_pre_check(sensor_id: str, request_data:Any = None, metadata: Dict[str, str] = None,  response_timeout_seconds=30, ) -> TaskingPreCheckResponse:
    """
    Performs a tasking precheck on the specified sensor

    Args:
        sensor_id (str): The sensor on which to perform the precheck
        response_timeout_seconds (int, optional): the number of seconds to wait for a successful TaskingPreCheckResponse
    Returns:
        response (TaskingPreCheckResponse): A SUCCESSFUL TaskingPreCheckResponse, or the last heard TaskingPreCheckResponse during the timeout period
    Raises:
        TimeoutError: Raises a TimeoutError if no TaskingPreCheckResponse message was heard during the timeout period
    """
    if request_data is not None:
        # Python proto -> python google Any proto
        python_any_request_data = Any()
        python_any_request_data.Pack(request_data)

        # python google Any proto -> python bytes
        request_data_byte_string = python_any_request_data.SerializeToString()

        # python bytes -> System Array[Byte]
        request_data_bytes = Array[Byte](request_data_byte_string)

        # bytes -> dotnet google Any proto
        request_data = Google.Protobuf.WellKnownTypes.Any()
        request_data = request_data.Parser.ParseFrom(request_data_bytes)

    dotnet_metadata=None
    if metadata is not None:
        # Convert metadata into a dotnet dictionary
        dotnet_metadata = Dictionary[String, String]()
        for k, v in metadata.items():
            dotnet_metadata[k] = v

    _task = __sdk_sensor.SensorTaskingPreCheck(sensorId=sensor_id, requestData=request_data, metaData=dotnet_metadata, responseTimeoutSecs=response_timeout_seconds)
    _task.Wait()

    # This converts the response from a dotnet object to a python object to insure transparent implementation
    response = TaskingPreCheckResponse()
    result_bytes = bytes(__sdk_utils.ConvertProtoToBytes(_task.Result))
    response.ParseFromString(result_bytes)

    return response


def sensor_tasking(sensor_id: str, request_data:Any = None, metadata: Dict[str, str] = None, response_timeout_seconds=30, ) -> TaskingResponse:
    """
    Performs a tasking on the specified sensor

    Args:
        sensor_id (str): The sensor on which to perform the tasking
        response_timeout_seconds (int, optional): the number of seconds to wait for a successful TaskingResponse
    Returns:
        response (TaskingResponse): A SUCCESSFUL TaskingResponse, or the last heard TaskingResponse during the timeout period
    Raises:
        TimeoutError: Raises a TimeoutError if no TaskingResponse message was heard during the timeout period
    """
    if request_data is not None:
        # Python proto -> python google Any proto
        python_any_request_data = Any()
        python_any_request_data.Pack(request_data)

        # python google Any proto -> python bytes
        request_data_byte_string = python_any_request_data.SerializeToString()
        
        # python bytes -> System Array[Byte]
        request_data_bytes = Array[Byte](request_data_byte_string)

        # bytes -> dotnet google Any proto
        request_data = Google.Protobuf.WellKnownTypes.Any()
        request_data = request_data.Parser.ParseFrom(request_data_bytes)

    # Convert metadata into a dotnet dictionary
    dotnet_metadata=None
    if metadata is not None:
        # Convert metadata into a dotnet dictionary
        dotnet_metadata = Dictionary[String, String]()
        for k, v in metadata.items():
            dotnet_metadata[k] = v

    _task = __sdk_sensor.SensorTasking(sensorId=sensor_id, requestData=request_data, metaData=dotnet_metadata,  responseTimeoutSecs=response_timeout_seconds)
    _task.Wait()

    # This converts the response from a dotnet object to a python object to insure transparent implementation
    response = TaskingResponse()
    result_bytes = bytes(__sdk_utils.ConvertProtoToBytes(_task.Result))
    response.ParseFromString(result_bytes)

    return response


def subscribe_to_sensor_data(callback_function: Callable[[T], None]):
    """
    Trigger a subscription to the sensor data event to process any incoming sensor data messages
    """
    global _sensor_data_subscribers
    _sensor_data_subscribers.append(callback_function)


def _sensor_data_handler(sensor_data):
    """
    Internal function to manage incoming sensorData message from the client app and do the proto transformation
    """
    global _sensor_data_subscribers
    response = SensorData()

    try:
        response.ParseFromString(bytes(sensor_data))
        for callback in _sensor_data_subscribers:
            Thread(target=callback, args=(response,)).start()
    except Exception as e:
        print(f"Error parsing sensor data: {e}")


__sdk_client.SensorDataEventPython += _sensor_data_handler
