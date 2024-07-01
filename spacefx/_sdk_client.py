import os

import pythonnet


def search_file(filename, search_path):
    """Search for a file recursively in a directory tree."""
    for dirpath, _, filenames in os.walk(search_path):
        for file in filenames:
            if file == filename:
                return os.path.join(dirpath, file)
    raise FileNotFoundError(f"{filename} not found in {search_path}")


prod_dotnet_dir = "/usr/share/dotnet/shared"
dev_dotnet_dir = "/root/.dotnet/shared"
dotnet_dir = ""

if os.path.exists(prod_dotnet_dir):
    dotnet_dir = prod_dotnet_dir
elif os.path.exists(dev_dotnet_dir):
    dotnet_dir = dev_dotnet_dir
else:
    raise ValueError(f"Was not able to find dotnet directory as '{prod_dotnet_dir}' nor '{dev_dotnet_dir}'.  Please check that dotnet is installed")

# Recursively search and load the runtimeconfig.json - this allows for dotnet minor version changes
runtimeconfig_file = search_file("Microsoft.AspNetCore.App.runtimeconfig.json", dotnet_dir)

pythonnet.load("coreclr", runtime_config=runtimeconfig_file)
import clr
from System.Reflection import Assembly

# Load the dotnet dlls by walking down the tree
for dirpath, dirnames, filenames in os.walk(os.path.join(dotnet_dir, 'Microsoft.AspNetCore.App')):
    if "runtimes" in dirpath:
        continue
    for file in filenames:
        if file.endswith(".dll"):
            Assembly.LoadFile(os.path.join(dirpath, file))

# Load the client adapter library which is in the parent directory / spacefxClient
for dirpath, dirnames, filenames in os.walk(os.path.join(os.path.dirname(__file__), 'spacefxClient')):
    if "runtimes" in dirpath:
        continue
    for file in filenames:
        if file.endswith(".dll"):
            dll_lib = Assembly.LoadFile(os.path.join(dirpath, file))
            if file == "sdk-dotnet.dll":
                assembly = clr.AddReference(os.path.join(dirpath, file))

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
