# Keyboard shortcut features

This page outlines the available keyboard shortcuts in Graph Toolkit.

The keyboard shortcuts feature in Graph Toolkit is integrated with Unity's editor shortcut system. As a result users can customize these shortcuts in the Unity Editor as follows: 

1. From the main menu, go to **Edit** > **Shortcuts** (macOS: **Unity** > **Shortcuts**).
1. In the **Category** column, select the name of the graph to customize the shortcuts bound to that graph.

For more details on how to work with shortcuts, refer to [Keyboard Shortcuts](https://docs.unity3d.com/Manual/ShortcutsManager.html).

The available keyboard shortcuts are as follows:

# Keyboard shortcut features

This page outlines the available keyboard shortcuts in Graph Toolkit.

The keyboard shortcuts feature in Graph Toolkit is integrated with Unity's editor shortcut system. As a result users can customize these shortcuts in the Unity Editor as follows: 
1. From the main menu, go to **Edit** > **Shortcuts** (macOS: **Unity** > **Shortcuts**).
1. In the **Category** column, select the name of the graph to customize the shortcuts bound to that graph.

For more details on how to work with shortcuts, refer to [Keyboard Shortcuts](https://docs.unity3d.com/Manual/ShortcutsManager.html).

The available keyboard shortcuts are as follows:

|Command       | Shortcut (Windows) | Shortcut (macOS)  | Description  |
|:-------------|:-------------------|:------------------|:-------------|
| Cut          | **Ctrl + X**         | **Cmd + X**         | Moves the selection from the [graph](glossary.md#graph) to the clipboard. |
| Copy         | **Ctrl + C**         | **Cmd + C**         | Copies the selection to the clipboard. |
| Paste        | **Ctrl + V**         | **Cmd + V**         | Pastes the content of the clipboard into your [graph](glossary.md#graph). |
| Duplicate    | **Ctrl + D**         | **Cmd + D**         | Pastes a copy of the current selection into the [graph](glossary.md#graph). |
| Duplicate without wires | **Ctrl + Shift + D** | **Cmd + Shift + D** | Pastes a copy of the current selection into the [graph](glossary.md#graph) without the connecting [wires](glossary.md#wire). |
| Delete       | **Del**              | **Cmd + Backspace** | Deletes the current selection. |
| Undo         | **Ctrl + Z**         | **Cmd + Z**         | Undoes the last action. |
| Redo         | **Ctrl + Y**         | **Cmd + Shift + Z**         | Redoes the last undone action. |
| Frame        | **F**                | **F**               | Focuses the view on the selected elements. If nothing is selected, focuses the view on all elements in the [graph](glossary.md#graph). |
| Frame All    | **A**                | **A**               | Focuses the view on all elements in the [graph](glossary.md#graph). |
| Frame Origin | **O**                | **O**               | Focuses the view on the origin (0,0) of the [graph](glossary.md#graph). |
| Convert Variable and Constant | **Ctrl + Shift + T** | **Cmd + Shift + T** | Converts the selected variable nodes to constant nodes, or vice versa. |
| Convert Wires to Portals | **Ctrl + Shift + P** | **Cmd + Shift + P** | Converts the selected [wires](glossary.md#wire) to [portals](glossary.md#portal). |
| Create Local Subgraph from Selection | **Ctrl + Shift + L** | **Cmd + Shift + L** | Converts the selected nodes into a local [subgraph](glossary.md#subgraph). |
| Create Sticky Note | **Alt + `**  | **Option + `**  | Creates a [sticky note](glossary.md#sticky-note) at the current mouse position. |
| Create Placemat | **Ctrl + Shift + M** | **Cmd + Shift + M** | Creates a [placemat](glossary.md#placemat) at the current mouse position. |
| Disconnect Wires | **Ctrl + Shift + W** | **Cmd + Shift + W** | Deletes all [wires](glossary.md#wire) on the selected [node](glossary.md#node). |
| Extract Contents To Placemat | **Ctrl + Shift + U** | **Cmd + Shift + U** | Extracts the contents of the selected [subgraph](glossary.md#subgraph) to a new [placemat](glossary.md#placemat). |
| Pan          |**Right-click + drag**| n/a               | Pans the graph view. |
| Select All   | **Ctrl + A**         | **Cmd + A**         | Selects all elements in the [graph](glossary.md#graph). |

## Additional resources
* [Pan and zoom features](pan-zoom-features.md)
* [Placemat features](placemat-features.md)
* [Portal features](portal-features.md)
* [Selection features](selection-features.md)
* [Sticky note features](sticky-note-features.md)
* [Subgraph features](subgraph-features.md)
* [ShortcutsManager](https://docs.unity3d.com/Manual/ShortcutsManager.html)
* [Undo/Redo](https://docs.unity3d.com/Manual/UndoWindow.html)