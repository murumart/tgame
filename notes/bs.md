Diplomacy: each faction has values and a view of the player faction. Making business with the other requires haggling resources in a way that the other feels they get an advantage from it. So if you're super strong they'd accept a loss in trade because complying would mean that you won't attack them.

---

- [X] Limited resource space is annoying to implement. Resources should be limited through availability and creation time, not storage.
- [ ] Jobs should somehow take workers automatically, because it's hard to manage it through AI. Or come up with a good way to manage it with AI...

### Sylvester

Games are played for their emotions - usually victory/defeat/suspense (arcade games). So game mechanics should be tools to bring out emotion, create emotional situations [video]. Emotions are provoked when an action changes a human value [book]. Human values: life/death, victory/defeat, friend/stranger/enemy, wealth/poverty, low status/high status, together/alone, love/ambivalence/hatred, freedom/slavery, danger/safety, knowledge/ignorance, skilled/unskilled, healthy/sick, and follower/leader [book]. Challenge is a common way to evoke emotion [book].

| 				          | Perceived by player | Not perceived by player     |
| -------------------     | ------------------- | -----------------------     |
| **Present in game**     | Normal              | ~~Unperceived complexity~~  |
| **Not present in game** | *!Apophenia*        | Normal                      |

Apophenia: human perception of emotions and intentions in everything. Need abstracted feedback and long-term relevance (so more opportunities to tie in context with what happens).

Disproportionate responses vs skill testing. DP for a good story. Losing something in the story makes for a good story, layers of failure states that you can get deeper into.

Plan for short periods and iterate. Make a dependency stack to see what parts of the game would need to change if another part changed and for what to implement first, throw others into a "later" pile.

#### Dependency stack

(depending objects are higher, baseline objects lower)
<pre>
* L A T E R * *
* [trade] []  *
* * * * * * * *

           [..region ui..]
           | deps on many|

[mandates]<----[complex resource production]
  |                     |         |
  |  [constructing buildings]     |
  |             |     |           |
[contracts]     |   [jobs]
     |          |                 |
[regions]<[map objects]     [workers]

</pre>

### Events

- Get a mandate for a resource you don't have but a neighboring (non-colony) region does. You have to then trade or extort it from them.
-