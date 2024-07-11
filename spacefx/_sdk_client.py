import os
import shutil
from pathlib import Path

import pythonnet


def search_file(filename, search_path):
    """Search for a file recursively in a directory tree using rglob."""
    search_path = Path(search_path)
    for file_path in search_path.rglob(filename):
        return str(file_path)
    return None


# Find the dotnet directory
DOTNET_BIN = shutil.which("dotnet")
if not DOTNET_BIN:
    raise ValueError(f"Unable to find an installation of dotnet.  Please install dotnet so it's found within the system PATH")

# Resolve the path to the dotnet binary
DOTNET_BIN = str(Path(DOTNET_BIN).resolve(strict=True))

# Find the shared directory
DOTNET_DIR = os.path.join(os.path.dirname(DOTNET_BIN), "shared")
if not os.path.exists(DOTNET_DIR):
    raise ValueError(f"dotnet was found at {DOTNET_BIN}, but unable to find the shared directory '{DOTNET_DIR}'.  Please check your dotnet installation and make sure the shared folder is present")


# Recursively search and load the runtimeconfig.json - this allows for dotnet minor version changes
runtime_config_file = search_file("Microsoft.AspNetCore.App.runtimeconfig.json", DOTNET_DIR)
if runtime_config_file is None:
    raise ValueError(f"Unable to find the runtimeconfig.json file for Microsoft.AspNetCore.App in the dotnet shared directory '{DOTNET_DIR}'")

# Load the runtimeconfig.json file
pythonnet.load("coreclr", runtime_config=runtime_config_file)
import clr
from System.Reflection import Assembly


# Load all dotnet dlls by walking down the Microsoft.AspNetCore.App tree
for dll_path in Path(os.path.join(DOTNET_DIR, 'Microsoft.AspNetCore.App')).rglob("*.dll"):
    if "runtimes" in dll_path.parts:
        continue
    Assembly.LoadFile(str(dll_path))


# Load the client adapter library
SPACEFX_CLIENT_DIR = os.path.join(os.path.dirname(__file__), 'spacefxClient')
spacesdk_client_dll = search_file("spacesdk-client.dll", SPACEFX_CLIENT_DIR)

if spacesdk_client_dll is None:
    raise ValueError(f"The DLL 'spacesdk-client.dll' was not found in '{SPACEFX_CLIENT_DIR}'. Please check that the client library was built and deployed to '{SPACEFX_CLIENT_DIR}'")

clr.AddReference(spacesdk_client_dll)


# Import the .NET SpaceFx namespace
import Microsoft.Azure.SpaceFx

# And you can create an instance of the Client class like this
__sdk_client = Microsoft.Azure.SpaceFx.SDK.Client
__sdk_core = Microsoft.Azure.SpaceFx.Core
__sdk_link = Microsoft.Azure.SpaceFx.SDK.Link
__sdk_logging = Microsoft.Azure.SpaceFx.SDK.Logging
__sdk_position = Microsoft.Azure.SpaceFx.SDK.Position
__sdk_sensor = Microsoft.Azure.SpaceFx.SDK.Sensor
__sdk_utils = Microsoft.Azure.SpaceFx.SDK.Utils