HOUSE VALIDATION

HouseSmoke.cs runs through Pipeline's script run command while HouseShop is in Play mode.
It replaces the running controller's storage with Temp/HouseUISave before starting a fixture.
It never writes the user's normal run. Exit Play mode when finished.
The fixture buys supplies through UI controls, borrows and examines an item with virtual mouse
input and physical raycasts, purchases it, performs the ritual, lists it on a shelf,
closes the night and checks the next-night save. It also checks doorway reachability.
The result is written to Temp/house-smoke-result.json.

HouseWalk.cs is an additional collision/navigation check. Run it only after HouseSmoke.cs
has completed in the same Play session, so storage is still isolated.
It moves the CharacterController along a route from the entrance to the desk, rug and shelves.
The result is written to Temp/house-walk-result.json.

The smoke test rotates the item into view programmatically and calls station interactions
through the controller. Surface examination and ritual actions use actual pointer state
and raycasts. This is not a substitute for a manual usability and art review.
