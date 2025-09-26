# DimViewer / DimSplitter 2.1

A Windows desktop application for converting images and PDF files into web-viewable tiled formats. DimViewer processes large images by splitting them into smaller tiles, creating interactive web viewers with zoom and pan functionality.

## Features

### File Processing
- **Multi-format support**: JPG, GIF, BMP, PNG, JPEG image files
- **PDF support**: Convert PDF pages to images using Ghostscript integration
- **Batch processing**: Handle multiple files simultaneously
- **File management**: Add, remove, and reorder files in the processing queue

### Image Processing
- **Tile generation**: Splits large images into 260x260 pixel tiles
- **Multiple zoom levels**: Creates 1-5 configurable zoom levels
- **Resolution control**: Configurable maximum resolution up to 300 DPI
- **Thumbnail generation**: Creates thumbnail versions for quick preview

### PDF Processing
- **PDF boundary boxes**: Supports MediaBox, CropBox, and TrimBox
- **Page extraction**: Converts individual PDF pages to image tiles
- **High-quality rendering**: Uses Ghostscript for professional PDF conversion

### Web Output
- **Interactive viewer**: Generates HTML/JavaScript web viewers
- **Zoom and pan**: Mouse-controlled navigation of large images
- **Responsive design**: Works across different screen sizes
- **Fast loading**: Tile-based loading for optimal performance

## System Requirements

- Windows OS (tested on Windows 7/8/10/11)
- .NET Framework 4.8
- Visual Studio 2012 or later (for development)
- Ghostscript (included as gsdll32.dll)

## Installation

### For End Users
1. Download the release package
2. Extract to your desired directory
3. Run `DimViewer.exe`
4. The application includes all required dependencies

### For Developers
1. Clone this repository
2. Open `DimViewer.sln` in Visual Studio
3. Build the solution (Debug or Release)
4. Run from Visual Studio or execute the built .exe file

## Usage

### Basic Workflow
1. **Add Files**: Click "Add Files" to select images or PDF files
2. **Configure Settings**:
   - Set output directory
   - Choose image prefix name
   - Adjust resolution settings (default: 180 DPI)
   - Select zoom levels (1-5)
3. **Process Files**: Click "Process" to start conversion
4. **View Results**: Navigate to the output directory to find your web viewer

### Configuration Options
- **Output Directory**: Where processed tiles and viewer will be saved
- **Image Prefix**: Naming convention for generated files
- **Max Resolution**: Maximum DPI for processed images (10-300)
- **Initial Dimensions**: Default viewer size (width/height)
- **Thumbnail Size**: Dimensions for thumbnail generation
- **Zoom Levels**: Number of zoom levels to generate (1-5)
- **PDF Box Type**: Choose between MediaBox, CropBox, or TrimBox

### File Management
- **Move Up/Down**: Reorder files in the processing queue
- **Remove**: Delete selected files from the queue
- **Clear All**: Remove all files from the queue
- **Rich Text**: Add text annotations to individual images

## Technical Details

### Architecture
- **Main Form**: `DimSplitter.cs` - Primary user interface and control logic
- **Tile Creation**: `CreateTiles.cs` - Core image processing and tile generation
- **PDF Processing**: `GhostscriptSharp.cs` - Ghostscript integration for PDF handling
- **Licensing**: `key.cs` - License validation system
- **Help System**: `Help.cs` - Built-in help and documentation

### Dependencies
- **Ghostscript**: PDF processing (gsdll32.dll)
- **iTextSharp**: PDF manipulation library
- **System.Drawing**: Image processing
- **Windows Forms**: User interface

### Output Structure
```
OutputDirectory/
├── tiles/
│   ├── zoom_level_0/
│   ├── zoom_level_1/
│   └── ...
├── thumbnails/
├── viewer.html
├── viewer.js
└── config.json
```

## Licensing

This project appears to have a licensing system:
- **Trial Version**: Includes watermarks on processed images
- **Licensed Version**: Removes watermarks and unlocks full functionality
- License validation through XML license files

## Development

### Building from Source
```bash
# Clone the repository
git clone [repository-url]

# Open in Visual Studio
# File -> Open -> Project/Solution -> DimViewer.sln

# Build the solution
# Build -> Build Solution (Ctrl+Shift+B)
```

### Project Structure
- `DimSplitter/` - Main application source code
- `Resources/` - Application resources (images, DLLs, license files)
- `Properties/` - Assembly information and project properties

### Key Files
- `DimSplitter.cs` - Main application form and logic
- `CreateTiles.cs` - Image processing and tile generation
- `GhostscriptSharp.cs` - PDF processing integration
- `DimViewer.csproj` - Visual Studio project file

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/new-feature`)
3. Commit your changes (`git commit -am 'Add new feature'`)
4. Push to the branch (`git push origin feature/new-feature`)
5. Create a Pull Request

## Support

For issues, questions, or feature requests, please create an issue in the GitHub repository.

## Version History

### Version 2.1
- Current stable release
- Multi-format image support
- PDF processing capabilities
- Web viewer generation
- Batch processing
- Configurable zoom levels

## Acknowledgments

- Uses Ghostscript for PDF processing
- Built with Windows Forms and .NET Framework
- Includes iTextSharp for PDF manipulation