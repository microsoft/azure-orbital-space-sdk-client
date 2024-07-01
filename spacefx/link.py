from spacefx.protos.link.Link_pb2 import LinkResponse

from spacefx._sdk_client import __sdk_link, __sdk_core, __sdk_utils


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


def send_file_to_app(destination_app_id: str, filepath: str, overwrite_destination_file=False, response_timeout_seconds=30) -> LinkResponse:
    """
    Sends a file to the destination service's inbox
    Args:
        destination_app_id (str): The app id of the service to which the message will be sent to
        filepath (str): Local file path of the input file to be pushed to hostsvc-link
        overwrite_destination_file (bool, optional): Flag to overwrite the file if it already exists at it's destination
        response_timeout_seconds (int, optional): the number of seconds to wait for a SUCCESSFUL LinkResponse
    Returns:
        response (LinkResponse): A SUCCESSFUL LinkResponse, or the last heard LinkResponse during the timeout period
    Raises:
        TimeoutError: Raises a TimeoutError if no LinkResponse message was heard during the timeout period
    """
    _task = __sdk_link.SendFileToApp(
        destinationAppId=destination_app_id,
        file=filepath,
        overwriteDestinationFile=overwrite_destination_file,
        responseTimeoutSecs=response_timeout_seconds
    )
    _task.Wait()

    response = LinkResponse()
    result_bytes = bytes(__sdk_utils.ConvertProtoToBytes(_task.Result))
    response.ParseFromString(result_bytes)

    return response


def downlink_file(destination_app_id: str, filepath: str, overwrite_destination_file=False, response_timeout_seconds=30) -> LinkResponse:
    """
    Sends a file to Message Translation Service to download the file to the ground at the next available opportunity
    Args:
        destination_app_id (str): The app id of the service to which the message will be sent to
        filepath (str): Local file path of the input file to be pushed to hostsvc-link
        overwrite_destination_file (bool, optional): Flag to overwrite the file if it already exists at it's destination
        response_timeout_seconds (int, optional): the number of seconds to wait for a SUCCESSFUL LinkResponse
    Returns:
        response (LinkResponse): A SUCCESSFUL LinkResponse, or the last heard LinkResponse during the timeout period
    Raises:
        TimeoutError: Raises a TimeoutError if no LinkResponse message was heard during the timeout period
    """
    _task = __sdk_link.DownlinkFile(
        destinationAppId=destination_app_id,
        file=filepath,
        overwriteDestinationFile=overwrite_destination_file,
        responseTimeoutSecs=response_timeout_seconds
    )
    _task.Wait()

    response = LinkResponse()
    result_bytes = bytes(__sdk_utils.ConvertProtoToBytes(_task.Result))
    response.ParseFromString(result_bytes)

    return response


def crosslink_file(destination_app_id: str, filepath: str, overwrite_destination_file=False, response_timeout_seconds=30) -> LinkResponse:
    """
    Crosslinks a file to the destination service's inbox
    Args:
        destination_app_id (str): The app id of the service to which the message will be sent to
        filepath (str): Local file path of the input file to be pushed to hostsvc-link
        overwrite_destination_file (bool, optional): Flag to overwrite the file if it already exists at it's destination
        response_timeout_seconds (int, optional): the number of seconds to wait for a SUCCESSFUL LinkResponse
    Returns:
        response (LinkResponse): A SUCCESSFUL LinkResponse, or the last heard LinkResponse during the timeout period
    Raises:
        TimeoutError: Raises a TimeoutError if no LinkResponse message was heard during the timeout period
    """
    _task = __sdk_link.CrosslinkFile(
        destinationAppId=destination_app_id,
        file=filepath,
        overwriteDestinationFile=overwrite_destination_file,
        responseTimeoutSecs=response_timeout_seconds
    )
    _task.Wait()

    response = LinkResponse()
    result_bytes = bytes(__sdk_utils.ConvertProtoToBytes(_task.Result))
    response.ParseFromString(result_bytes)

    return response
