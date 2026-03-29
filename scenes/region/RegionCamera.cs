using Godot;
using scenes.autoload;
using scenes.region.ui;

namespace scenes.region;

public partial class RegionCamera : Camera {

	[Export] public Node2D Cursor;
	[Export] Node2D debugCursor;
	[Export] public RegionDisplayHighlight RegionDisplayHighlight;
	[Export] RegionDisplay regionDisplay;
	[Export] UI ui;

	public Region Region;


	public override void _Ready() {
		base._Ready();
		RemoveChild(ui);
		UILayer.AddUIChild(ui);
		ClickedMouseEvent += (ac) => ui.OnLeftMouseClick(ac, regionDisplay.GetTilePosFromLocalPos(ac));

		// debug
		//GetTree().Connect(SceneTree.SignalName.ProcessFrame, Callable.From(() => {
		//	RegionDisplayHighlight.SetDisplay(Region.GetEdgesHighlightFunction(GameMan.Game.Map.TileOwners));
		//}), (uint)ConnectFlags.OneShot);
	}

	public override void _Process(double delta) {
		base._Process(delta);
		MouseHighlight();
	}

	protected override bool MouseButtonInput(InputEventMouseButton evt) {
		if (base.MouseButtonInput(evt)) return true;
		if (evt.ButtonIndex == MouseButton.Right && evt.IsPressed()) {
			var wPos = GetCanvasTransform().AffineInverse() * evt.Position;
			ui.OnRightMouseClick(wPos, regionDisplay.LocalToTile(wPos));
			return true;
		}
		return false;
	}

	private Vector2I lastTilePos;
	private void MouseHighlight() {
		debugCursor.Visible = !dragging;
		Cursor.Visible = !dragging;
		var tilepos = regionDisplay.GetMouseHoveredTilePos();
		if (tilepos != lastTilePos) {
			var tileWorldPos = regionDisplay.Region.WorldPosition + tilepos;
			Cursor.Position = Tilemaps.TilePosToWorldPos(tilepos) + Vector2.Up * Tilemaps.TileElevationVerticalOffset(tileWorldPos, GameMan.Game.Map.World);
			debugCursor.Position = Tilemaps.TilePosToWorldPos(tilepos);
			RegionDisplayHighlight.Update(GameMan.Game.Map.World, tilepos, tileWorldPos);
			ui.OnTileHighlighted(tilepos, Region);
			lastTilePos = tilepos;
		}
	}

	public Vector2I GetMouseHoveredTilePos() => regionDisplay.GetMouseHoveredTilePos();
	public Vector2I GetHoveredTilePos() => regionDisplay.GetTilePosFromLocalPos(Position);

}
