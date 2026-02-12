using System;
using System.Collections.Generic;
using Godot;

namespace scenes.region.resource_mines {

	public partial class General : MapObjectView {

		[Export] Sprite2D sprite;

		Dictionary<string, Texture2D> textures = new(){
			{"rock", GD.Load<Texture2D>("res://scenes/region/resource_mines/boulder.png")},
			{"broadleaf woods", GD.Load<Texture2D>("res://scenes/region/resource_mines/broadleaf_woods.png")},
			{"clay pit", GD.Load<Texture2D>("res://scenes/region/resource_mines/clay_pit.png")},
			{"conifer woods", GD.Load<Texture2D>("res://scenes/region/resource_mines/conifer_woods.png")},
			{"rubble", GD.Load<Texture2D>("res://scenes/region/resource_mines/rubble.png")},
			{"savanna trees", GD.Load<Texture2D>("res://scenes/region/resource_mines/savanna_trees.png")},
			{"rainforest trees", GD.Load<Texture2D>("res://scenes/region/resource_mines/rainforest_trees.png")},
			{"fishing spot", GD.Load<Texture2D>("res://scenes/region/resource_mines/fishing_spot.png")},
		};


		public override void _Ready() {
			base._Ready();
			Debug.Assert(sprite != null);
			Debug.Assert(textures.ContainsKey(mapObjectRef.Type.AssetName), $"No mapobjectview texture set up for {mapObjectRef.Type.AssetName}");
			sprite.Texture = textures[mapObjectRef.Type.AssetName];
		}

	}

}

