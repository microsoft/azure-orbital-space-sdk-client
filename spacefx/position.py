from spacefx.protos.position.Position_pb2 import PositionResponse

from spacefx._sdk_client import __sdk_position, __sdk_utils


def request_position(response_timeout_seconds=30) -> PositionResponse:
    """
    Requests the lasts observed position from hostsvc-position

    Args:
        response_timeout_seconds (int, optional): the number of seconds to wait for a successful PositionResponse
    Returns:
        response (PositionResponse): A SUCCESSFUL or NOT_FOUND PositionResponse, or the last heard PositionResponse during the timeout period
    Raises:
        TimeoutError: Raises a TimeoutError if no PositionResponse message was heard during the timeout period
    """
    _task = __sdk_position.LastKnownPosition(responseTimeoutSecs=response_timeout_seconds)
    _task.Wait()

    # This converts the response from a dotnet object to a python object to insure transparent implementation
    response = PositionResponse()
    result_bytes = bytes(__sdk_utils.ConvertProtoToBytes(_task.Result))
    response.ParseFromString(result_bytes)

    return response
