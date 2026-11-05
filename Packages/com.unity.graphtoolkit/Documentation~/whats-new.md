# What's New in Unity Graph Toolkit 0.4.0-exp.1

[!INCLUDE [Experimental Warning message](experimental-release.md)]

This update adds new default keyboard shortcuts to improve workflow and efficiency for your graphs, adds context (right-click) menus,
and includes various bug fixes.

Discover new features and performance improvements in the latest update to Unity Graph Toolkit. 

For more information, refer to the [CHANGELOG](../CHANGELOG.md).


## New keyboard shortcuts

Added the following default keyboard shortcuts for graphs. 

|Command      | Shortcut (Windows) | Shortcut (macOS) | Description |
|:-------------|:---------------------|:------------------|:-------------|
| Create Local Subgraph from Selection | `Ctrl + Shift + L` | `Cmd + Shift + L` | Converts the selected nodes into a local subgraph. |
| Pan |`Right-click + drag` | n/a | Pans the graph view. |
 
Users can customize these shortcuts in the Unity Editor as follows: 
1. From the main menu, go to **Edit** > **Shortcuts** (macOS: **Unity** > **Shortcuts**).
1. In the **Category** column, select the name of the graph to customize the shortcuts bound to that graph.

For more details on how to work with shortcuts, refer to [Keyboard Shortcuts](https://docs.unity3d.com/Manual/ShortcutsManager.html).

## New context menus 

Added context (right-click) menus to the following areas of the graph editor:

* The Blackboard and variables on the blackboard
* Context nodes and block nodes
* Graph canvas
* Nodes and State nodes
* Placemats
* Ports
* Sticky notes
* Subgraph nodes
* Variable and constant nodes in the graph canvas (separate from variables on the blackboard)
* Wires

In addition, the following changes were made to context menus:

* Contextual menus now show only the commands that are common to the selected items, instead of combining all menu commands.
* When you right-click on an empty part of the graph canvas, the graph canvas menu opens, even if graph elements are selected.
