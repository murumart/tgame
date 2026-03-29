extends Label

const AFFIRMATIONS: PackedStringArray = [
	"You Are Beautiful And Perfect Honey",
	"Go Get them Girlboss",
	"Today is a good day to kick some ass",
	"Show them what we're made of!",
	"War is really cool!",
	"Let's shell them while they sleep!",
	"I love starting my morning with a fresh cup of kicked ass."
]


func _ready() -> void:
	text = AFFIRMATIONS[randi() % AFFIRMATIONS.size()]
