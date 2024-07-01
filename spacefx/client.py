from time import sleep

from spacefx._sdk_client import __sdk_client
from spacefx._sdk_client import __sdk_core


def build():
    """
    Microsoft.Azure.SpaceFx entry and initialization point.
    Initializes the GRPC channel to the Dapr sidecar.

    Args:
        None
    Returns:
        Built __sdk_client.Client
    """
    return __sdk_client.Build()


def services_online() -> []:
    """
    Returns the services that have transmitted heartbeats to this service
    """
    services = __sdk_client.ServicesOnline()
    return services


def wait_for_sidecar(timeout_period=None):
    """
    Waits until the sidecar is active, up to a timeout period

    Args:
        timeout_period (int, optional): how long to wait for the sidecar to activate, in seconds. Defaults to 30 seconds.
    Returns:
        response (str): a string representation of the sidecar status (Microsoft.Azure.SpaceFx.Core.Enums.SIDECAR_STATUS.ToString())
    Raises:
        TimeoutException
    """
    return __sdk_client.WaitForOnline(timeout_period)


def keep_app_open():
    """
    Utility function to prevent the app from closing
    """
    while True:
        sleep(1)


def get_app_id() -> str:
    """
    Retrieves the application ID associated with this servie

    Returns:
        response (str): the app id registered
    Raises:
        TimeoutError: Raises a TimeoutError if no PositionResponse message was heard during the timeout period
    """
    _task = __sdk_core.GetAppID()
    _task.Wait()

    return _task.Result

def get_config_dir() -> str:
    """
    Retrieves the path to the configuration directory

    Returns:
        response (str): the path to the configuration directory
    """
    _task = __sdk_core.GetConfigDirectory()
    _task.Wait()

    return _task.Result



def get_config_setting(config_file_name: str) -> str:
    """
    Retrieves a configuration setting by reading the supplied filename under the config directory

    Args:
        config_file_name (str): name of the configuration file to read
    Returns:
        response (str): configuration file contents
    """
    _task = __sdk_core.GetConfigSetting(configFileName=config_file_name)
    _task.Wait()

    return _task.Result

