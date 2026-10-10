local panel_id = "hello-world"

ffo.ui.register_panel({
    id = panel_id,
    title = "Hello World",
    text = "Waiting to enter the world..."
})

ffo.events.subscribe("player.entered_world", function(event)
    ffo.ui.set_panel_text(panel_id, "Welcome, " .. event.player.display_name .. "!")
end)
