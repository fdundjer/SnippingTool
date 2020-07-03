# Snipping tool
Simple Windows application which grabs screenshot of your screen. Supports multiple monitors.

## Startup Arguments
Application has to be started with arguments. 
Supported arguments:

```
-f - Path to a file to which screenshot will be saved (required).
-e - Escape key that will be used to close the overlay (optional). Default ESC. 
```

For possible list of keys check [System.Windows.Input.Key](https://docs.microsoft.com/en-us/dotnet/api/system.windows.input.key?view=netcore-3.1)

## Overlay
When started overlay will appear across all screens.

![overlay](overlay.gif)

When overlay is active, you can move the selected area by holding 'SPACE' key.