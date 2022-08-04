# File Verification

File Verification is a portable application designed to computer the checksums for all files in a directory and subdirectory.

The checksums are stored in a text file in the same directory as the files. This checksum file can then be used to verify that the files haven't been changed when File Verification is run against that same directory.

## Features

File Verification includes the following:

**Generate MD5, SHA1, SHA256 or SHA512 hash value.** Specific which hash algorithm to use for all files in a directory or subdirectory.

**Exclude specific files or folders.** Files and folders can be excluded from having their checksum computed.

**Send notification to an API endpoint.** Send an API request to an endpoint once the checksums have been computed.

**Verify the checksums.** Verify the checksums stored in the checksum file still match the current checksum for the files in the directory and subdirectories.

**Verify checksum from downloaded files.** Verify a checksum from a file downloaded from the Internet by passing in the file path, hash algorithm and checksum value.

**Display checksum for a files.** Quickly display the checksum value for a file for any support hash algorithm.

**Portable** No installation is required. Download the [latest release](https://github.com/TechieGuy12/FileVerification/releases/latest) and unzip the contents to a folder.

**Fast performance.** Multi-threading increases the number of files that can be processed at a time.

## System Support

- Windows
- MacOS
- Linux

For information using File Verification, please read the [Wiki](https://github.com/TechieGuy12/FileVerification/wiki).
