# FileVerification

The basic function of File Verification is to generate the checksum values of all files in a directory and save those checksums to a text file that is located in the same directory. Once the checksums are saved to the file, running File Verification against the same directory will validate that the files in the directory still match the checksums saved in the checksum file.

In addition, File Verification can also validate the checksum of a single file by accepting both the file and checksum as arguments.

For a quick checksum validation, File Verification can simply display the checksum of a file to the console.

# Arguments

The following arguments can be passed into File Verification:

| Argument | Description |
| --------- | ----------- |
| -f, --file &lt;_file_&gt; | (Required) The file or folder to generate the checksum. |
| -a, --algorithm &lt;_MD5,SHA1,SHA256,SHA512_&gt; | The hash algorithm used to generate the checksum. Default: SHA256. |
| -ha, --hash &lt;_hash_&gt; | The hash used to validate against the file specified with the -f argument. |
| -t, --threads &lt;_threads_&gt; | The number of threads to use to verify the file(s). Default: number of processors. |
| -ho, --hashonly | Generate and display the file hash - doesn't save the hash to the checksum file. |
| - sfi, --settingsFile &lt;_settingsFile_&gt; | The name of the settings XML file. |
| - sfo, --settingsFolder &lt;_settingsFolder_&gt; | The folder containing the settings file. |
| --version | Version information. |
| -?, -h, --help | Show the help and usage information. |

When either the `hash` or `hashonly` arguments are specified, the checksum of the file is not saved to the checksum file. The `file` attribute must contain the location of a file and not a folder.

# Settings File

The settings file is used to specify additional settings that aren't passed in from the command line. Currently, the XML structure of the settings file is as follows:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<settings>
    <notifications>
        <waittime></waittime>
        <notification>
            <url></url>
            <method></method>
            <data>
                <headers>
                    <header>
                        <name></name>
                        <value></value>
                    </header>
                </headers>
                <body></body>
            </data>
        </notification>
    </notifications>
</settings>
```

## Notification Elements

To send a notification request to an endpoint, the following information can be specified:

| Element | Description |
| --------- | ----------- |
| url | The URL to connect to for the notification. |
| method | The HTTP method to use for the request. Default: POST |
| data | Data to send for the request. |

### URL

This is the valid URL to the endpoint and is specified using the `<url>` element.

### Method

The `<method>` element specifies the HTTP method used in the request to the endpoint. The valid values are:
| Method |
| --------- |
| POST |
| GET |
| PUT |
| DELETE |
>**Note:** The method names are case-sensitive, so they must be added to the configuration file exactly as shown in the table above.

The default value for the `<method>` element is `POST`.

### Data

The `<data>` element contains information that is sent to the endpoint. This element contains the `<headers>`, `<body>`, and `<type>` child elements to provide details about the data sent with the request.

#### Headers

The `<headers>` element allows you to specify various headers to include in the request. Each header is specified within a `<header>` child element, and contains a `<name>` and `<value>` pair of elements. For example:

```xml
<headers>
    <header>
        <name>HeaderName</name>
        <value>HeaderValue</value>
    </header>
</headers>
```

### Body

The `<body>` element provides information to send in the request. You can specify any message in the `<body>` element, or you can use the `[message]` placeholder to have File Watcher write the change message into the body.

## Examples

Generate the checksums for all files located in `C:\Temp`:

`fv.exe -f C:\Temp`

Validate that the checksum of a file called `notes.txt` in `C:\Temp` matches `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855`:

`fv.exe -f C:\Temp\notes.txt -ha E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855`

Use 32 threads when generating or validating the hashes of files in a directory:

`fv.exe -f C:\Temp -t 32`