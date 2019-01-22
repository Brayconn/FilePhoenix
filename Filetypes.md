# Legend

| Heading | Description |
| --- | --- |
| File Type | The name of the file type |
| Extension(s) | What file extensions this format uses |
| Level Of Compliance | How closely FilePhoenix can parse the file according to the file type's specification |
| Tested | Whether or not compatibility has actually been tested with this file type |
| Sections To Databend | Parts of the file relevant to databenders, usually you'll want the file labed "data" that meets this condition |

# Supported File Types

| File Type | Extension(s) | Level Of Compliance | Tested | Sections To Databend | 
| --- | --- | --- | --- | --- |
| 3rd Generation Partnership Program | 3g2, 3gp, 3gpp | Basic | Yes | Boxes of type "mdat", "moov", and "trak" |
| Adobe Flash Protected Audio/Video | f4a, f4b, f4p, f4v | Basic | No | Boxes of type "mdat", "moov", and "trak" |
| Animated Portable Network Graphics | apng | Full? | No | Chunks of type "IDAT" |
| Apple DRM Protected Video | m4v | Basic | No | Boxes of type "mdat", "moov", and "trak" |
| Apple Quicktime | mov, qt | Basic | No | ??? |
| Digital Video Broadcasting | dvb | Basic | No | Boxes of type "mdat", "moov", and "trak" |
| Graphics Interchange Format | gif | High | Mostly | Any Image data section |
| Jeff's Image Format | jif | High | Mostly | Any Image data section |
| JPEG 2000 | jp2, jpm, jpx | Basic | No | ??? |
| JPEG Network Graphics | jng | Full? | No | Chunks of type "IDAT" and "JDAT" |
| Microsoft Cursor Files | cur | Full | Yes | The actual image files (.png should be [chained](Tips.md)) |
| Microsoft Icon Files | ico | Full | Yes | The actual image files (.png should be [chained](Tips.md)) |
| MPEG-4 Audio | m4a, m4b, m4p | Basic | No | ??? |
| MPEG-4 Video | mp4 | Basic (Only one level of parsing) | Yes | Boxes of type "mdat", "moov", and "trak" |
| Motion JPEG 2000 | mj2, mjp2 | Basic | No | ??? |
| Multiple Network Graphics | mng | High (no validation for dpngs) | No | Chunks of type "IDAT" and "JDAT" |
| Portable Network Graphics | png | Full? | Yes | Chunks of type "IDAT" |
| Sony Movie Format | mqv | Basic | No | Boxes of type "mdat", "moov", and "trak" |