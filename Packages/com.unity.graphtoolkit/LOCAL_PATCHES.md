# Local fixes for Graph Toolkit 0.4.0-exp.2

This package is embedded through Unity Package Manager so fixes survive a Library rebuild.

## Frame All during view reconstruction

GraphView.CalculateRectToFitAll previously called ChangeCoordinatesTo on the parent
of a detached view. Scheduled framing and minimap updates could therefore throw
ArgumentNullException (src). An exception also prevented cleanup of its static
scratch list, retaining stale view references.

The method now ignores detached views and views outside its content container,
clears the scratch list before collection, and clears it in a finally block.

Regression coverage: Assets/Dialogue/Tests/Editor/GraphFramingTests.cs.
When upgrading/replacing this embedded package, check whether the upstream version
contains an equivalent fix before removing this patch.

## Unity 6000.3 callback overload compatibility

ConstantField.GetRegisterCallback used SingleOrDefault on the method name and
number of generic arguments. Unity 6000.3.22f1 exposes both TrickleDown and
CallbackOptions overloads with two generic arguments. The ambiguous lookup threw
before node/blackboard fields could be constructed, leaving partial/detached views.
The lookup now selects the three-parameter overload ending in TrickleDown,
matching the existing invocation. The graph-window tests exercise field creation.

## Node option input sizing

Field.uss reserves at least 220 pixels for input controls inside
.ge-node__node-options, with a minimum property width of 380 pixels.
This prevents labels from squeezing text and numeric inputs down to a few pixels.
