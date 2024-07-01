#!/bin/bash
#
#  Contains the post build steps used by this repo for python debug and wheel generation
#  This is called by the csproj file and should not be called directly
#
# arguments:
#
#   debug - Debug configuration requested
#   release - Release configuration requested
#   target-dir - Directory to place the files
#
# Example Usage:
#
#  "bash ./vscode/postBuild [--debug] [--release] --target-dir ./tmp/someDirectory"


# If we're running in the devcontainer with the k3s-on-host feature, source the .env file
[[ -f "/devfeature/k3s-on-host/.env" ]] && source /devfeature/k3s-on-host/.env

# Pull in the app.env file built by the feature
[[ -n "${SPACEFX_DEV_ENV}" ]] && [[ -f "${SPACEFX_DEV_ENV}" ]] && source "${SPACEFX_DEV_ENV:?}"


#-------------------------------------------------------------------------------------------------------------
source "${SPACEFX_DIR:?}/modules/load_modules.sh" $@ --log_dir "${SPACEFX_DIR:?}/logs/${APP_NAME:?}"

cd /workspaces/spacesdk-client

############################################################
# Script variables
############################################################
DEBUG=false
RELEASE=false
TARGET_DIR=""
PYTHON_DLL_DIR="/workspaces/spacesdk-client/spacefx/spacefxClient"
DOTNET_DLL_BUILD_DIR_PREFIX="/workspaces/spacesdk-client/src/bin"
DOTNET_DLL_BUILD_DIR_SUFFIX="/net6.0"
DOTNET_DLL_BUILD_DIR=""
############################################################
# Help                                                     #
############################################################
function show_help() {
   # Display Help
   echo "Contains the post build steps used by this repo for python debug and wheel generation."
   echo
   echo "Syntax: bash ./vscode/postBuild [--debug] [--release] --output_dir ./tmp/someDirectory"
   echo "options:"
   echo "--debug                            [OPTIONAL] Debug configuration requested"
   echo "--release                          [OPTIONAL] Release configuration requested"
   echo "--target-dir                       [OPTIONAL] Directory to place the files"
   echo "--help | -h                        [OPTIONAL] Help script (this screen)"
   echo
   exit 1
}



############################################################
# Process the input options. Add options as needed.        #
############################################################
# Get the options

while [[ "$#" -gt 0 ]]; do
    case $1 in
        -h|--help) show_help ;;
        --target-dir)
            shift
            TARGET_DIR=$1
            if [[ ! ${TARGET_DIR:0:1} == "/" ]]; then
                TARGET_DIR="${CONTAINER_WORKING_DIR}/src/${TARGET_DIR}"
            fi
            ;;
        --debug)
            DEBUG=true
            DOTNET_DLL_BUILD_DIR="${DOTNET_DLL_BUILD_DIR_PREFIX}/Debug${DOTNET_DLL_BUILD_DIR_SUFFIX}"
            ;;
        --release)
            RELEASE=true
            DOTNET_DLL_BUILD_DIR="${DOTNET_DLL_BUILD_DIR_PREFIX}/Release${DOTNET_DLL_BUILD_DIR_SUFFIX}"
            ;;
        *) echo "Unknown parameter passed: $1"; show_help ;;
    esac
    shift
done

############################################################
# Clean out the python dll directory
############################################################
function clean_python_dir() {
    info_log "START: ${FUNCNAME[0]}"

    info_log "Cleaning python dll directory '${PYTHON_DLL_DIR}'..."
    run_a_script "rm -rf ${PYTHON_DLL_DIR}/*.dll"
    run_a_script "rm -rf ${PYTHON_DLL_DIR}/*.pdb"
    run_a_script "rm -rf ${PYTHON_DLL_DIR}/*.json"

    run_a_script "rm -rf ${CONTAINER_WORKING_DIR}/dist/*"

    info_log "...successfully cleaned python dll directory '${PYTHON_DLL_DIR}'"

    info_log "END: ${FUNCNAME[0]}"
}

############################################################
# Copy outputs to the python spacefx directory
############################################################
function copy_for_python() {
    info_log "START: ${FUNCNAME[0]}"

    info_log "Copying '${DOTNET_DLL_BUILD_DIR}/*' to '${PYTHON_DLL_DIR}/'..."
    run_a_script "cp -r ${DOTNET_DLL_BUILD_DIR}/* ${PYTHON_DLL_DIR}/"

    info_log "...successfully copied '${DOTNET_DLL_BUILD_DIR}/*' to '${PYTHON_DLL_DIR}/'."

    info_log "END: ${FUNCNAME[0]}"
}

############################################################
# Generates the python wheel when a Release configuration is requested
############################################################
function generate_wheel_for_release() {
    info_log "START: ${FUNCNAME[0]}"

    info_log "Generating wheel with poetry..."
    run_a_script "/root/.local/bin/poetry build"
    info_log "...wheel successfully generated"

    info_log "Copying '${CONTAINER_WORKING_DIR}/dist/*' to target directory '${TARGET_DIR}'..."
    run_a_script "mkdir -p ${TARGET_DIR}"
    run_a_script "cp -r ${CONTAINER_WORKING_DIR}/dist/* ${TARGET_DIR}/"
    info_log "...successfully copied '${CONTAINER_WORKING_DIR}/dist/*' to target directory '${TARGET_DIR}'"

    info_log "END: ${FUNCNAME[0]}"
}

function main() {
    write_parameter_to_log DEBUG
    write_parameter_to_log RELEASE
    write_parameter_to_log TARGET_DIR

    clean_python_dir
    copy_for_python

    if [[ "${RELEASE}" == true ]]; then
        generate_wheel_for_release

    fi


    info_log "------------------------------------------"
    info_log "END: ${SCRIPT_NAME}"
}

main
