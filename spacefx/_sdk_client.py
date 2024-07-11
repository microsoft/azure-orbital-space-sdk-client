import os
import pythonnet
import shutil
from pathlib import Path

def search_file(filename, search_path):
    """Search for a file recursively in a directory tree."""
    for dirpath, _, filenames in os.walk(search_path):
        for file in filenames:
            if file == filename:
                return os.path.join(dirpath, file)
    raise FileNotFoundError(f"{filename} not found in {search_path}")

# Find the dotnet directory
DOTNET_DIR = shutil.which("dotnet")
if not DOTNET_DIR:
    raise ValueError(f"Unable to find an installation of dotnet.  Please install dotnet so it's found within the system PATH")

DOTNET_DIR = Path(DOTNET_DIR).resolve(strict=True).parent
DOTNET_DIR = os.path.join(DOTNET_DIR, "shared")

if not os.path.exists(DOTNET_DIR):
    raise ValueError(f"dotnet was found, but unable to find the shared directory '${DOTNET_DIR}'.  Please check your dotnet installation and make sure the shared folder is present")


# Recursively search and load the runtimeconfig.json - this allows for dotnet minor version changes
runtimeconfig_file = search_file("Microsoft.AspNetCore.App.runtimeconfig.json", DOTNET_DIR)

pythonnet.load("coreclr", runtime_config=runtimeconfig_file)
import clr
from System.Reflection import Assembly

# Load the dotnet dlls by walking down the tree
for dirpath, dirnames, filenames in os.walk(os.path.join(DOTNET_DIR, 'Microsoft.AspNetCore.App')):
    if "runtimes" in dirpath:
        continue
    for file in filenames:
        if file.endswith(".dll"):
            Assembly.LoadFile(os.path.join(dirpath, file))


# Load the client adapter library which is in the parent directory / spacefxClient
base_dir = Path(__file__).parent / 'spacefxClient'

# Find "spacesdk-client.dll" in any subdirectory
spacesdk_client_dll = next(base_dir.rglob('spacesdk-client.dll'), None)

# Found it.  Add it
if spacesdk_client_dll:
    assembly = clr.AddReference(str(spacesdk_client_dll))
else:
    raise ValueError(f"The DLL 'spacesdk-client.dll' was not found in '{str(base_dir)}'. Please check that the client library was built and deployed to '{str(base_dir)}'")


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
