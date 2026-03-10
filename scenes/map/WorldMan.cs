using System.Linq;
using Godot;
using scenes.autoload;
using scenes.map.ui;
using scenes.ui;

namespace scenes.map {

	public partial class WorldMan : Node {

		[Export] WorldGenerator worldGenerator;
		[Export] WorldRenderer worldRenderer;
		[Export] PackedScene regionScene;
		[Export] WorldUI worldUI;
		[Export] Camera camera;

		static Map Map => GameMan.Singleton.Game.Map;
		static World World => Map.World;


		public override void _Ready() {
			if (worldUI != null && worldUI.GetParent() == this) {
				RemoveChild(worldUI);
				UILayer.AddUIChild(worldUI);
			}
			worldUI.SelectRegion(null);
			worldUI.RegionPlayRequested += EnterGame;
			worldUI.WorldTileInfoRequested += where => {
				var w = new Vector2I((int)where.X, (int)where.Y);
				if (Map == null) return (-3f, -3f, -3f);
				if (Map.World == null) return (-2f, -2f, -2f);
				return (Map.World.GetElevation(w.X, w.Y), Map.World.GetTemperature(w.X, w.Y), Map.World.GetHumidity(w.X, w.Y));
			};

			camera.ClickedMouseEvent += MouseClicked;

			worldUI.ResourceDisplay.Display(c => {
				if (GetViewport() == null) (c as Label).Text =  "...";
				var mousePos = (Vector2I)camera.GetMousePos();
				(c as Label).Text =  $"{mousePos}";
			});
			worldUI.ResourceDisplay.Display(c => (c as Label).Text = $"seed: {World?.Seed ?? 1377}");
		}

		void MouseClicked(Vector2I pos) {
			if (Map == null || worldGenerator.Generating) return;
			if (!Map.TileOwners.TryGetValue(pos, out Region region)) return;
			worldUI.SelectRegion(region);
		}

		public override void _Process(double delta) {
			var mousePos = (Vector2I?)camera?.GetMousePos() ?? Vector2I.Zero;
			if (Map != null && !worldGenerator.Generating) {
				Map.TileOwners.TryGetValue(mousePos, out Region region);
				worldRenderer.DrawRegionHighlight(region, worldUI.SelectedRegion);
			}
		}

		public override void _ExitTree() {
			worldUI.QueueFree();
		}

		void RegionClicked(Region region) {
			worldUI.SelectRegion(region);
		}

		void SetupGame() {
			GameMan.Singleton.NewGame(Map);
			GD.Print("WorldMan::SetupGame : game set up.");
		}

		void EnterGame() {
			GameMan.Singleton.Game.PlayRegion = worldUI.SelectedRegion;
			GetTree().ChangeSceneToPacked(regionScene);
		}

	}

}
