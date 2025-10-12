extends MarginContainer

# one big script to rule all region ui interactions


signal build_target_set(what: int)

enum Tab {
	BUILD,
}

# bottom bar buttons
@onready var build_button: Button = %BuildButton
@onready var policy_button: Button = %PolicyButton
@onready var world_button: Button = %WorldButton

# bottom bar menus menus
@onready var menu_tabs: TabContainer = %MenuTabs

@onready var build_menu_list: ItemList = %BuildMenuList
@onready var build_menu_confirmation: Button = %BuildMenuConfirmation

var _selected_build_thing_ix: int = -1


# overrides and connections

func _ready() -> void:
	build_button.pressed.connect(_on_build_button_pressed)
	build_menu_list.item_activated.connect(_on_build_thing_confirmed)
	build_menu_list.item_selected.connect(_on_build_thing_selected)
	build_menu_confirmation.pressed.connect(_on_build_thing_confirmed)

	_reset()


func _on_build_button_pressed() -> void:
	if menu_tabs.current_tab != Tab.BUILD:
		_select_tab(0)
	else:
		_select_tab(-1)


func _on_build_thing_selected(which: int) -> void:
	build_menu_confirmation.disabled = false
	_selected_build_thing_ix = which
	build_menu_confirmation.text = "Build " + build_menu_list.get_item_text(which)


func _on_build_thing_confirmed(which: int = -1) -> void:
	if which == -1: # pressed button, didnt doubleclick
		which = _selected_build_thing_ix
	_selected_build_thing_ix = which
	build_target_set.emit(_selected_build_thing_ix)
	_selected_build_thing_ix = -1
	_select_tab(-1)


# menu activites

func _select_tab(which: int) -> void:
	if which == -1:
		# reset some things
		build_menu_confirmation.disabled = true
		build_menu_confirmation.text = "select"
		_selected_build_thing_ix = -1
	menu_tabs.current_tab = which


# utilities

func _reset() -> void:
	menu_tabs.current_tab = -1
	build_menu_confirmation.disabled = true
