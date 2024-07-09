import os
from pathlib import Path
import pythonnet


def find_git_root(starting_directory):
    """
    Search for a .git directory by moving up the directory tree from the starting directory.
    If found, return the path to the directory containing the .git directory.
    If not found and the root of the directory tree is reached, return None.
    """
    current_directory = starting_directory
    while True:
        # Check if .git directory exists in the current directory
        if os.path.isdir(os.path.join(current_directory, ".git")):
            return os.path.join(current_directory, ".git")
        # Move up one directory level
        parent_directory = os.path.dirname(current_directory)
        if parent_directory == current_directory:
            # Root of the directory tree is reached without finding .git
            return None
        current_directory = parent_directory


def search_file(filename, search_path):
    """Search for a file recursively in a directory tree."""
    for dirpath, _, filenames in os.walk(search_path):
        for file in filenames:
            if file == filename:
                return os.path.join(dirpath, file)
    raise FileNotFoundError(f"{filename} not found in {search_path}")

# Find the dotnet directory
PROD_DOTNET_DIR = "/usr/share/dotnet/shared"
DOTNET_DIR=""

# SpaceFX-Dev installed dotnet to the .git directory so it can be used by the
# debugShim and the devcontainer.  This will locate the .git directory by
# walking up the tree
DEV_DOTNET_DIR = find_git_root(os.path.dirname(os.path.abspath(__file__)))

if DEV_DOTNET_DIR:
    DEV_DOTNET_DIR = os.path.join(DEV_DOTNET_DIR, "spacefx-dev", "dotnet", "shared")
    DOTNET_DIR = DEV_DOTNET_DIR if os.path.exists(DEV_DOTNET_DIR) else None

if not DOTNET_DIR:
    DOTNET_DIR = PROD_DOTNET_DIR if os.path.exists(PROD_DOTNET_DIR) else None

if not DOTNET_DIR:
    raise ValueError(f"Was not able to find dotnet directory as '{PROD_DOTNET_DIR}' nor within a '{DEV_DOTNET_DIR}'. Please check that dotnet is installed")


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
