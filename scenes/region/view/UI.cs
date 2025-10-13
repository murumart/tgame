using Godot;
using System;


namespace scenes.region.view {
	public partial class UI : Control {

		// one big script to rule all region ui interactions

		[Signal]
		public delegate void BuildTargetSetEventHandler(int target);

		public enum Tab {
			BUILD,
		}

		// bottom bar buttons
		[Export] public Button buildButton;
		[Export] private Button policyButton;
		[Export] public Button worldButton;


		// bottom bar menus menus
		[Export] public TabContainer menuTabs;

		[Export] public ItemList buildMenuList;
		[Export] public Button buildMenuConfirmation;

		private long selectedBuildThingId = -1;


		// overrides and connections

		public override void _Ready() {

			buildButton.Pressed += OnBuildButtonPressed;
			buildMenuList.ItemActivated += OnBuildThingConfirmed;
			buildMenuList.ItemSelected += OnBuildThingSelected;
			buildMenuConfirmation.Pressed += OnBuildThingConfirmed;

			Reset();
		}

		private void OnBuildButtonPressed() {
			if (menuTabs.CurrentTab != (int)Tab.BUILD) {
				SelectTab(0);
			} else {
				SelectTab(-1);
			}
		}

		private void OnBuildThingSelected(long which) {
			buildMenuConfirmation.Disabled = false;
			selectedBuildThingId = which;
			buildMenuConfirmation.Text = "Build " + buildMenuList.GetItemText((int)which);

		}

		private void OnBuildThingConfirmed() {
			// pressed button, didnt doubleclick
			OnBuildThingConfirmed(selectedBuildThingId);
		}

		private void OnBuildThingConfirmed(long which) {
			selectedBuildThingId = which;
			EmitSignal(SignalName.BuildTargetSet, selectedBuildThingId);
			selectedBuildThingId = -1;
			SelectTab(-1);
		}

		// menu activites

		private void SelectTab(long which) {
			if (which == -1) {
				// reset some things
				buildMenuConfirmation.Disabled = true;
				buildMenuConfirmation.Text = "select";
				selectedBuildThingId = -1;
			}
			menuTabs.CurrentTab = (int)which;
		}

		// utilities

		private void Reset() {
			menuTabs.CurrentTab = -1;
			buildMenuConfirmation.Disabled = true;
		}

	}
}
