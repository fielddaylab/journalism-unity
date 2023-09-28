# journalism-unity
Unity implementation of Journalism Game

# Logging Event Schema

## Progression

level_begin(level_id)

    Level 6 is for the credits

begin_story(level_id, snippits_available[])

level_complete(level_id, snippits_used[], alignment_score, quality_score)

## Player Events

choose_node(node_id)

advance_script(node_id)

view_status

close_status

view_story

close_story

view_editor_notes

close_editor_notes

add_snippit_to_story(snippit_id, story_slot)

delete_snppit_from_story(snippit_id, story_slot)

## Game and Feedback Events

display_node(node_id)

display_choices(choices[node_id, choice_text, destination_node_id])
