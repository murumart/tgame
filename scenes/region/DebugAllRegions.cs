using System;
using Godot;
using scenes.autoload;
using scenes.map.ui;
using scenes.region;

public partial class DebugAllRegions : Node {

	[Export] Node2D regionParent;
	[Export] RegionCamera camera;
	[Export] RegionDisplay dummyStupid;
	[Export] WorldGenUi worldGenUi;


	public override void _Ready() {
		camera.SetProcess(false);
		worldGenUi.GoBackEvent += GenMap;
	}

	public override void _UnhandledKeyInput(InputEvent e) {
		if (e is not InputEventKey k) return;
		if (k.Keycode == Key.Alt && k.Pressed) {
			worldGenUi.Visible = !worldGenUi.Visible;
		}
	}

	void GenMap() {
		worldGenUi.Hide();
		camera.SetProcess(true);
		foreach (var r in regionParent.GetChildren()) r.QueueFree();
		dummyStupid.LoadRegion(GameMan.Singleton.Game.Map.GetRegion(0), 2, camera);
		Callable.From(() => {
			foreach (var region in GameMan.Singleton.Game.Map.GetRegions()) {
				var rdisp = RegionDisplay.Instantiate();
				regionParent.AddChild(rdisp);
				rdisp.Modulate = region.LocalFaction.Color.Lightened(0.99f);
				rdisp.Position = Tilemaps.TilePosToWorldPos(region.WorldPosition) - Tilemaps.TILE_SIZE / 2;
				rdisp.LoadRegion(region, 0, camera);
			}

		}).CallDeferred();
	}

}
