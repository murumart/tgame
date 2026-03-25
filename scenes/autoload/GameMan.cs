using System;
using System.Collections.Generic;
using Godot;
using resources.game;
using Environment = System.Environment;

namespace scenes.autoload;

public partial class GameMan : Node {

	[Export] DataStorage dataRegistry;

	static GameMan singleton;
	public static Map DebugMap { get; private set; }
	Game game;
	public static Game Game => singleton.game;

	public enum GameSpeedChanger {
		UI,
	}

	readonly Dictionary<GameSpeedChanger, float> gameSpeedMults = new();
	float gameSpeed = 1f;
	bool paused = true;
	public static bool IsPaused => singleton.paused;
	public static float GameSpeed => singleton.gameSpeed;

	static bool _sceneTransitioning = false;
	public static bool IsSceneTransitioning => _sceneTransitioning;


	public override void _Ready() {
		Debug.Assert(singleton == null, "There GameMan can only GameMan Be One");
		singleton = this;

		dataRegistry.RegisterThings();
		GD.Print("GameMan::_Ready : GameMan is set up");

		DebugMap = Map.GetDebugMap();
		NewGame(DebugMap);
		Game.SetPlayRegion(DebugMap.GetRegion(0));
		GD.Print("GameMan::_Ready : debug map generated");
	}

	double timeAccum = 0.0;
	const float gameMinutesPerRealSeconds = 2.0f;
	public override void _Process(double delta) {
		if (paused) return;
		float speed = gameSpeed;
		double toPass = delta * gameMinutesPerRealSeconds * (GameTime.SECS_TO_HOURS * GameTime.MINUTES_PER_HOUR) * speed;
		timeAccum += toPass;
		if (timeAccum >= 1) {
			Game.PassTime(1);
			timeAccum -= 1;
		}
		while (timeAccum >= 5) {
			Game.PassTime(1);
			timeAccum -= 1;
		}
	}

	public static void MultiplyGameSpeed(GameSpeedChanger who, float amount) {
		singleton.gameSpeedMults[who] = amount;
		float a = 1f;
		foreach (float mult in singleton.gameSpeedMults.Values) a *= mult;
		singleton.gameSpeed = a;
	}

	public static void TogglePause() {
		singleton.paused = !singleton.paused;
	}

	public static void NewGame(Map map) {
		singleton.game?.Deinit();
		singleton.game = null;
		GC.Collect(); // please die
		singleton.game = new Game(map);
		if (singleton.game.Time.Minutes == 0) singleton.game.PassTime(60 * 7); // start game at 7:00
	}

	public static async void SceneTransition(string to) {
		Debug.Assert(to != "" && to.EndsWith(".tscn"), "Scene name invalid!");
		Debug.Assert(ResourceLoader.Exists(to), "Scene doesn't exist!");
		SceneTransition(GD.Load<PackedScene>(to));
	}

	public static async void SceneTransition(PackedScene to) {
		Debug.Assert(!_sceneTransitioning, "Don't transition scene while already transitioning!");

		_sceneTransitioning = true;
		await UILayer.BeginTransitionAnimation();
		await singleton.ToSignal(singleton.GetTree(), SceneTree.SignalName.ProcessFrame);
		Node root = singleton.GetTree().Root;
		Node currentScene = root.GetChild(-1);
		root.RemoveChild(currentScene);
		await singleton.ToSignal(singleton.GetTree(), SceneTree.SignalName.ProcessFrame);
		currentScene.QueueFree();

		await singleton.ToSignal(singleton.GetTree(), SceneTree.SignalName.ProcessFrame);
		Node newScene = to.Instantiate();
		root.AddChild(newScene);
		await singleton.ToSignal(singleton.GetTree(), SceneTree.SignalName.ProcessFrame);

		await UILayer.EndTransitionAnimation();
		_sceneTransitioning = false;
	}

}
