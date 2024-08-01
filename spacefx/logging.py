from typing import Union
import uuid
import logging

from spacefx.protos.common.Common_pb2 import \
    LogMessageResponse, \
    TelemetryMetricResponse, \
    TelemetryMultiMetricResponse

import Microsoft.Azure.SpaceFx.MessageFormats.Common
from spacefx._sdk_client import __sdk_logging, __sdk_utils


def send_log_message(message: str, log_level: Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL = Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Trace, response_timeout_seconds: int = 30, wait_for_response: bool = False) -> LogMessageResponse:
    """
    Sends a message to the Logging Host Service

    Args:
        message (str): message that will be logged within hostsvc-logging
        log_level (LOG_LEVEL, optional): log level that the message will be logged under
        response_timeout_seconds (int, optional): the number of seconds to wait for a successful LogMessageResponse
        wait_for_response (bool, optional): enable/disable whether or not to wait for a LogMessageResponse from the Logging Service.  Disabled by default.
    Returns:
        response (LogResponse): A successful LogMessageResponse, or the last heard LogMessageResponse during the timeout period
    Raises:
        TimeoutError: Raises a TimeoutError if no LogResponse message was heard during the timeout period
    """
    log_message = Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage()
    log_message.LogLevel = Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL(log_level.value__)
    log_message.Message = message

    return send_complex_log_message(log_message=log_message, response_timeout_seconds=response_timeout_seconds, wait_for_response=wait_for_response)


def send_complex_log_message(log_message: Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage, response_timeout_seconds:int = 30, wait_for_response:bool = False) -> LogMessageResponse:
    """
    Sends a message to the Logging Host Service

    Args:
        message (str): message that will be logged within hostsvc-logging
        log_level (LOG_LEVEL, optional): log level that the message will be logged under
        response_timeout_seconds (int, optional): the number of seconds to wait for a successful LogMessageResponse
        wait_for_response (bool, optional): enable/disable whether or not to wait for a LogMessageResponse from the Logging Service.  Disabled by default.
    Returns:
        response (LogResponse): A successful LogMessageResponse, or the last heard LogMessageResponse during the timeout period
    Raises:
        TimeoutError: Raises a TimeoutError if no LogResponse message was heard during the timeout period
    """
    log_message.RequestHeader = log_message.RequestHeader or Microsoft.Azure.SpaceFx.MessageFormats.Common.RequestHeader()
    log_message.RequestHeader.TrackingId = log_message.RequestHeader.TrackingId or str(uuid.uuid4())
    log_message.RequestHeader.CorrelationId = log_message.RequestHeader.CorrelationId or log_message.RequestHeader.TrackingId

    _task = __sdk_logging.SendLogMessage(logMessage=log_message, responseTimeoutSecs=response_timeout_seconds, wait_for_response=wait_for_response)
    _task.Wait()

    response = LogMessageResponse()
    result_bytes = bytes(__sdk_utils.ConvertProtoToBytes(_task.Result))
    response.ParseFromString(result_bytes)

    return response


def send_telemetry(metric_name_or_object: Union[str, Microsoft.Azure.SpaceFx.MessageFormats.Common.TelemetryMetric], metric_value: int = None, response_timeout_seconds: int = 30, wait_for_response: bool = False) -> TelemetryMetricResponse:
    """
    Sends a telemetry message to the Logging Host Service

    Args:
        metric_name_or_object (Union[str, Microsoft.Azure.SpaceFx.MessageFormats.Common.TelemetryMetric]):
            Either the metric name (str) or the Telemetry Metric object to be sent.
        metric_value (int, optional): Value of the metric to send. Required if metric_or_log_message is a str.
        response_timeout_seconds (int, optional): the number of seconds to wait for a successful TelemetryMetricResponse
        wait_for_response (bool, optional): enable/disable whether or not to wait for a TelemetryMetricResponse from the Logging Service.  Disabled by default.
    Returns:
        response (TelemetryMetricResponse): The TelemetryMetricResponse received from Logging Service
    Raises:
        TimeoutError: Raises a TimeoutError if no response message was heard during the timeout period
    """

    if isinstance(metric_name_or_object, str):
        if metric_value is None:
            raise ValueError("metric_value must be provided when sending a telemetry metric.")

        # Send telemetry metric
        _task = __sdk_logging.SendTelemetry(metricName=metric_name_or_object, metricValue=metric_value, responseTimeoutSecs=response_timeout_seconds, wait_for_response=wait_for_response)
        _task.Wait()


    elif isinstance(metric_name_or_object, Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMeTelemetryMetricssage):
        # Send log message
        telemetry_message = metric_name_or_object
        telemetry_message.RequestHeader = telemetry_message.RequestHeader or Microsoft.Azure.SpaceFx.MessageFormats.Common.RequestHeader()
        telemetry_message.RequestHeader.TrackingId = telemetry_message.RequestHeader.TrackingId or str(uuid.uuid4())
        telemetry_message.RequestHeader.CorrelationId = telemetry_message.RequestHeader.CorrelationId or telemetry_message.RequestHeader.TrackingId

        # Assuming similar logic to send the log message and wait for response
        _task = __sdk_logging.SendTelemetry(telemetryMessage=telemetry_message, responseTimeoutSecs=response_timeout_seconds, wait_for_response=wait_for_response)
        _task.Wait()


    response = TelemetryMetricResponse()
    result_bytes = bytes(__sdk_utils.ConvertProtoToBytes(_task.Result))
    response.ParseFromString(result_bytes)
    return response

def send_telemetrymulti(telemetry_multi: Microsoft.Azure.SpaceFx.MessageFormats.Common.TelemetryMultiMetric, response_timeout_seconds: int = 30, wait_for_response: bool = False) -> TelemetryMultiMetricResponse:
    """
    Sends a telemetry message to the Logging Host Service

    Args:
        telemetryMulti (Microsoft.Azure.SpaceFx.MessageFormats.Common.TelemetryMultiMetric): The TelemetryMultiMetric object to be sent.
        response_timeout_seconds (int, optional): the number of seconds to wait for a successful TelemetryMetricResponse
        wait_for_response (bool, optional): enable/disable whether or not to wait for a TelemetryMetricResponse from the Logging Service.  Disabled by default.
    Returns:
        response (TelemetryMultiMetricResponse): TelemetryMultiMetricResponse received from Logging Service
    Raises:
        TimeoutError: Raises a TimeoutError if no response message was heard during the timeout period
    """

        # Send log message
    telemetry_multi.RequestHeader = telemetry_multi.RequestHeader or Microsoft.Azure.SpaceFx.MessageFormats.Common.RequestHeader()
    telemetry_multi.RequestHeader.TrackingId = telemetry_multi.RequestHeader.TrackingId or str(uuid.uuid4())
    telemetry_multi.RequestHeader.CorrelationId = telemetry_multi.RequestHeader.CorrelationId or telemetry_multi.RequestHeader.TrackingId

    # Assuming similar logic to send the log message and wait for response
    _task = __sdk_logging.SendMultiTelemetry(telemetryMessage=telemetry_multi, responseTimeoutSecs=response_timeout_seconds, wait_for_response=wait_for_response)
    _task.Wait()


    response = TelemetryMultiMetricResponse()
    result_bytes = bytes(__sdk_utils.ConvertProtoToBytes(_task.Result))
    response.ParseFromString(result_bytes)
    return response

# This is inteded to be used as a drop-in replacement for the default python logger class
# Use via spacefx.logger rather than accessing the logger directly
class __SpaceFxLogger(logging.getLoggerClass()):
    def __init__(self, name="SpaceFxLogger", level=logging.NOTSET):
        super().__init__(name, level)
        handler = logging.StreamHandler()
        formatter = logging.Formatter('%(asctime)s - %(levelname)s - %(message)s')
        handler.setFormatter(formatter)
        self.addHandler(handler)
        self._level = level

    def _log(self, level, msg, *args, **kwargs):
        """Let adapters modify the message and keyword arguments."""
        return super()._log(level, msg, *args, **kwargs)

    def debug(self, msg, *args, **kwargs):
        self.send_log_message(msg, Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Debug)
        super().debug(msg, *args, **kwargs)

    def info(self, msg, *args, **kwargs):
        self.send_log_message(msg, Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Info)
        super().info(msg, *args, **kwargs)

    def warning(self, msg, *args, **kwargs):
        self.send_log_message(msg, Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Warning)
        super().warning(msg, *args, **kwargs)

    def error(self, msg, *args, **kwargs):
        self.send_log_message(msg, Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Error)
        super().error(msg, *args, **kwargs)

    def critical(self, msg, *args, **kwargs):
        self.send_log_message(msg, Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Critical)
        super().critical(msg, *args, **kwargs)

    def send_log_message(self, msg, level: Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL):
        logMsg = Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage()
        logMsg.Message = msg
        logMsg.LogLevel = level
        send_complex_log_message(logMsg)
